using Task = Core.Entities.Task;

namespace Core.Abstractions.Repositories
{
    public interface ITaskRepository : IRepository<Task, Guid>
    {
        // Add task-specific methods if needed
    }
}
