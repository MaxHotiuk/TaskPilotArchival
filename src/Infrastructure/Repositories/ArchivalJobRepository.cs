using Microsoft.Azure.Cosmos;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

using Core.Abstractions.Repositories;

public class ArchivalJobRepository : IArchivalJobRepository
{
    private readonly Container _container;
    private readonly ILogger<ArchivalJobRepository> _logger;

    public ArchivalJobRepository(CosmosClient cosmosClient, string databaseId, string containerId, ILogger<ArchivalJobRepository> logger)
    {
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _logger = logger;
    }

    public async Task<ArchivalJob?> GetJobAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<ArchivalJob>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Job not found: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading job: {Id}", id);
            throw;
        }
    }

    public async Task UpsertJobAsync(ArchivalJob job)
    {
        try
        {
            await _container.UpsertItemAsync(job, new PartitionKey(job.BoardId.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting job: {Id}", job.id);
            throw;
        }
    }

    public async Task UpdateJobStatusAsync(string id, string partitionKey, string status, string? errorMessage = null)
    {
        var job = await GetJobAsync(id, partitionKey);
        if (job == null) return;
        job.Status = status;
        job.ErrorMessage = errorMessage;
        await UpsertJobAsync(job);
    }
}
