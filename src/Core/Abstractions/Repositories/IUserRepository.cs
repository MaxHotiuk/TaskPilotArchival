using Core.Entities;

namespace Core.Abstractions.Repositories
{
    public interface IUserRepository : IRepository<User, Guid>
    {
        // Add user-specific methods if needed
    }
}
