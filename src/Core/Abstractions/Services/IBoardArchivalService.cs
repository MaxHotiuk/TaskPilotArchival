namespace Core.Abstractions.Services;

using Core.Models;
using System.Threading;
using System.Threading.Tasks;

public interface IBoardArchivalService
{
    Task ArchiveBoardAsync(BoardArchivalMessage message, CancellationToken cancellationToken = default);
}
