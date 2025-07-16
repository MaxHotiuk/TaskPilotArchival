using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Infrastructure.Configuration;
using System.Diagnostics;

namespace Infrastructure.Services;

using Core.Abstractions.Services;
using Core.Models;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly StorageSharedKeyCredential? _sharedKeyCredential;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;
    private string? _accountName;
    private string? _accountKey;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureBlob:ConnectionString"] ?? Environment.GetEnvironmentVariable("BlobStorageConnection") ?? throw new InvalidOperationException("BlobStorageConnection not set");
        _containerName = configuration["AzureBlob:ContainerName"] ?? throw new InvalidOperationException("AzureBlob:ContainerName not set");
        _blobServiceClient = new BlobServiceClient(connectionString);
        ExtractAccountCredentials(connectionString);
        if (_accountName != null && _accountKey != null)
        {
            _sharedKeyCredential = new StorageSharedKeyCredential(_accountName, _accountKey);
        }
    }

    private void ExtractAccountCredentials(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountName="))
                _accountName = part.Substring("AccountName=".Length);
            else if (part.StartsWith("AccountKey="))
                _accountKey = part.Substring("AccountKey=".Length);
        }
    }

    public string GenerateBlobSasToken(string fileName, TimeSpan expiration)
        => GenerateBlobSasToken(fileName, expiration, BlobSasPermissions.Read);

    public string GenerateBlobSasToken(string fileName, TimeSpan expiration, BlobSasPermissions permissions)
    {
        if (_sharedKeyCredential == null)
            throw new InvalidOperationException("Storage credentials not initialized");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = fileName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiration)
        };

        sasBuilder.SetPermissions(permissions);

        var blobUriBuilder = new BlobUriBuilder(_blobServiceClient.Uri)
        {
            BlobContainerName = _containerName,
            BlobName = fileName,
            Sas = sasBuilder.ToSasQueryParameters(_sharedKeyCredential)
        };

        return blobUriBuilder.ToUri().ToString();
    }

    public string GenerateAccountSasToken(TimeSpan expiration, AccountSasPermissions permissions, AccountSasResourceTypes resourceTypes)
    {
        if (_sharedKeyCredential == null)
            throw new InvalidOperationException("Storage credentials not initialized");

        var sasBuilder = new AccountSasBuilder
        {
            Services = AccountSasServices.Blobs,
            ResourceTypes = resourceTypes,
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiration)
        };

        sasBuilder.SetPermissions(permissions);

        return sasBuilder.ToSasQueryParameters(_sharedKeyCredential).ToString();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var containerClient = await GetOrCreateContainerAsync(cancellationToken);
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return GenerateBlobSasToken(fileName, TimeSpan.FromHours(1));
    }

    public async Task<Stream?> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = await GetBlobClientAsync(fileName, cancellationToken);
        if (blobClient == null)
            return null;
        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(fileName);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var results = new List<string>();
        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            results.Add(blobItem.Name);
        }
        return results;
    }

    public async Task<BlobFileMetadata?> GetFileMetadataAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = await GetBlobClientAsync(fileName, cancellationToken);
        if (blobClient == null)
            return null;
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        return CreateBlobFileMetadata(fileName, fileName, properties.Value);
    }

    public async Task<IEnumerable<BlobFileMetadata>> ListFilesWithMetadataAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var results = new List<BlobFileMetadata>();
        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var fileName = blobItem.Name.Substring(prefix.Length);
            results.Add(CreateBlobFileMetadata(fileName, blobItem.Name, properties.Value));
        }
        return results;
    }

    private BlobContainerClient GetContainerClient()
        => _blobServiceClient.GetBlobContainerClient(_containerName);

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        return containerClient;
    }

    private async Task<BlobClient?> GetBlobClientAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(fileName);
        if (!await blobClient.ExistsAsync(cancellationToken))
            return null;
        return blobClient;
    }

    private BlobFileMetadata CreateBlobFileMetadata(string fileName, string blobName, BlobProperties properties)
    {
        return new BlobFileMetadata
        {
            FileName = fileName,
            BlobName = blobName,
            Url = GenerateBlobSasToken(blobName, TimeSpan.FromHours(1)),
            ContentType = properties.ContentType,
            Size = properties.ContentLength,
            UploadedAt = properties.CreatedOn.UtcDateTime
        };
    }
}
