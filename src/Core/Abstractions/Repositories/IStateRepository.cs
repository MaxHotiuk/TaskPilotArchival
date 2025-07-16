using Core.Entities;

namespace Core.Abstractions.Repositories
{
    public interface IStateRepository : IRepository<State, int>
    {
        // Add state-specific methods if needed
    }
}
