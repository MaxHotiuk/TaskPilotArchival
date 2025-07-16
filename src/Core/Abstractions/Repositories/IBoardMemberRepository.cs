using Core.Entities;

namespace Core.Abstractions.Repositories
{
    public interface IBoardMemberRepository : IRepository<BoardMember, (Guid, Guid)>
    {
        // Add board member-specific methods if needed
    }
}
