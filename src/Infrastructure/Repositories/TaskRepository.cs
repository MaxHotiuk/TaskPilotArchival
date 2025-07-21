using Core.Abstractions.Repositories;
using Core.Entities;
using TaskEntity = Core.Entities.Task;

namespace Infrastructure.Repositories
{
    public class TaskRepository : Repository<TaskEntity, Guid>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext dbContext) : base(dbContext) { }
        // Add task-specific methods if needed
    }
}
