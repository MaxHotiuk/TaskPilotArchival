using Core.DTOs;

namespace Core.Abstractions.Services;

public interface ICosmosArchivalStorageService
{
    Task SaveArchivedBoardAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken = default);
    Task<BoardArchivalDto> GetArchivedBoardAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task DeleteArchivedBoardAsync(Guid boardId, CancellationToken cancellationToken = default);
}
