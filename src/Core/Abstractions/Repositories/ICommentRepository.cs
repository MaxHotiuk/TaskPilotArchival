using Core.Entities;

namespace Core.Abstractions.Repositories
{
    public interface ICommentRepository : IRepository<Comment, Guid>
    {
        // Add comment-specific methods if needed
    }
}
