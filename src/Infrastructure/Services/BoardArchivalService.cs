using Microsoft.Extensions.Logging;
using Infrastructure.Helpers;
using Core.Models;

namespace Infrastructure.Services;

using Core.Abstractions.Services;

public class BoardArchivalService : IBoardArchivalService
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<BoardArchivalService> _logger;

    public BoardArchivalService(BlobStorageService blobStorageService, ILogger<BoardArchivalService> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task ArchiveBoardAsync(BoardArchivalMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var boardInfo = new BoardInfo
            {
                BoardId = message.BoardId,
                BoardName = message.BoardName,
                ArchivedAt = DateTime.UtcNow
            };

            var json = SerializationHelper.SerializeToJson(boardInfo);
            var blobName = $"archivals/{boardInfo.BoardId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            await _blobStorageService.UploadFileAsync(stream, blobName, "application/json", cancellationToken);
            _logger.LogInformation("Board archived and uploaded to blob: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving board {BoardId}", message.BoardId);
            throw;
        }
    }
}
