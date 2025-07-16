using Core.Abstractions.Repositories;
using Core.Entities;

namespace Infrastructure.Repositories
{
    public class BoardRepository : Repository<Board, Guid>, IBoardRepository
    {
        public BoardRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        // Add board-specific methods if needed
    }
}
