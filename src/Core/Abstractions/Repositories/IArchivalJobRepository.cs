namespace Core.Abstractions.Repositories;

using Core.Models;
using System.Threading.Tasks;

public interface IArchivalJobRepository
{
    Task<ArchivalJob?> GetJobAsync(string id, string partitionKey);
    Task UpsertJobAsync(ArchivalJob job);
    Task UpdateJobStatusAsync(string id, string partitionKey, string status, string? errorMessage = null);
}
