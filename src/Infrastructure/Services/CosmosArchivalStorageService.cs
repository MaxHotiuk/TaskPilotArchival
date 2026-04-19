using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Core.Abstractions.Services;
using Core.DTOs;
using Core.Models;

namespace Infrastructure.Services;

public class CosmosArchivalStorageService : ICosmosArchivalStorageService
{
    private readonly Container _container;
    private readonly ILogger<CosmosArchivalStorageService> _logger;

    public CosmosArchivalStorageService(
        CosmosClient cosmosClient,
        string databaseId,
        string containerId,
        ILogger<CosmosArchivalStorageService> logger)
    {
        _container = cosmosClient.GetContainer(databaseId, containerId);
        _logger = logger;
    }

    public async Task SaveArchivedBoardAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken = default)
    {
        var document = ArchivedBoardDocument.FromDto(boardDto);

        await _container.UpsertItemAsync(
            document,
            new PartitionKey(document.PartitionKey),
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Board {BoardId} archived and saved to Cosmos DB container '{Container}'.",
            boardDto.BoardId,
            _container.Id);
    }

    public async Task<BoardArchivalDto> GetArchivedBoardAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var id = boardId.ToString();

        try
        {
            var response = await _container.ReadItemAsync<ArchivedBoardDocument>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);

            return response.Resource.BoardData;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "No archived document found in Cosmos DB for board {BoardId}.",
                boardId);

            throw new InvalidOperationException(
                $"No archived document found for board {boardId}.", ex);
        }
    }

    public async Task DeleteArchivedBoardAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var id = boardId.ToString();

        try
        {
            await _container.DeleteItemAsync<ArchivedBoardDocument>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Archived document for board {BoardId} deleted from Cosmos DB container '{Container}'.",
                boardId,
                _container.Id);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Attempted to delete archived document for board {BoardId}, but it was not found. Skipping.",
                boardId);
        }
    }
}
