using Core.Abstractions.Repositories;
using Core.Entities;

namespace Infrastructure.Repositories
{
    public class StateRepository : Repository<State, int>, IStateRepository
    {
        public StateRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        // Add state-specific methods if needed
    }
}
