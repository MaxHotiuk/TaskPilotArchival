using Core.Abstractions.Repositories;
using Core.Entities;

namespace Infrastructure.Repositories
{
    public class CommentRepository : Repository<Comment, Guid>, ICommentRepository
    {
        public CommentRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        // Add comment-specific methods if needed
    }
}
