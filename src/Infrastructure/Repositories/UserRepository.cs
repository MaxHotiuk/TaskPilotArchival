using Core.Abstractions.Repositories;
using Core.Entities;

namespace Infrastructure.Repositories
{
    public class UserRepository : Repository<User, Guid>, IUserRepository
    {
        public UserRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        // Add user-specific methods if needed
    }
}
