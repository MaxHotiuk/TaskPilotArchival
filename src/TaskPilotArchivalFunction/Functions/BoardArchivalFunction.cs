using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Models;
using Infrastructure.Services;

namespace TaskPilotArchivalFunction.Functions;

public class BoardArchivalFunction
{
    private readonly BoardArchivalService _archivalService;
    private readonly ILogger<BoardArchivalFunction> _logger;

    public BoardArchivalFunction(BoardArchivalService archivalService, ILogger<BoardArchivalFunction> logger)
    {
        _archivalService = archivalService;
        _logger = logger;
    }

    [Function("BoardArchivalFunction")]
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

            await _archivalService.ArchiveBoardAsync(archivalMessage, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Service Bus message: {Message}", message);
            throw;
        }
    }
}
