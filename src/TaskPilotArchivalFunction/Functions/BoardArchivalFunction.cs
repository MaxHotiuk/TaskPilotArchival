using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Models;
using Core.Abstractions.Services;

namespace TaskPilotArchivalFunction.Functions;

public class BoardArchivalFunction
{
    private readonly IBoardArchivalService _archivalService;
    private readonly ILogger<BoardArchivalFunction> _logger;

    public BoardArchivalFunction(IBoardArchivalService archivalService, ILogger<BoardArchivalFunction> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    [Function(nameof(BoardArchivalFunction))]
    public async Task RunAsync([
        ServiceBusTrigger("board-archival-queue", Connection = "ServiceBusConnection")
    ] string message, FunctionContext context)
    {
        try
        {
            var archivalMessage = JsonSerializer.Deserialize<BoardArchivalMessage>(message);
            if (archivalMessage == null)
            {
                _logger.LogWarning("Received null or invalid message.");
                return;
            }

            switch (archivalMessage.JobType)
            {
                case "BoardArchival":
                    await _archivalService.ArchiveBoardAsync(archivalMessage, context.CancellationToken);
                    break;
                case "BoardDearchival":
                    if (_archivalService is IBoardDearchivalService dearchivalService)
                    {
                        await dearchivalService.DearchiveBoardAsync(archivalMessage, context.CancellationToken);
                    }
                    else
                    {
                        _logger.LogError("Dearchival service not implemented.");
                        throw new NotImplementedException("Dearchival service not implemented.");
                    }
                    break;
                default:
                    _logger.LogWarning($"Unknown JobType: {archivalMessage.JobType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Service Bus message: {Message}", message);
            throw;
        }
    }
}
