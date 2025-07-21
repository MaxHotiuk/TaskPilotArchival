using Core.Entities;

namespace Core.Abstractions.Repositories
{
    public interface IBoardRepository : IRepository<Board, Guid>
    {
        // Add board-specific methods if needed
    }
}
