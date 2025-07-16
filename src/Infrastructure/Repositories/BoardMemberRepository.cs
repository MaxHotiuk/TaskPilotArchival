using Core.Abstractions.Repositories;
using Core.Entities;

namespace Infrastructure.Repositories
{
    public class BoardMemberRepository : Repository<BoardMember, (Guid, Guid)>, IBoardMemberRepository
    {
        public BoardMemberRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        // Add board member-specific methods if needed
    }
}
