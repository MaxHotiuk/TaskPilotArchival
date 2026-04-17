using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Core.Abstractions.Repositories;
using Infrastructure.Repositories;
using Core.Abstractions.Services;
using Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    // Register ApplicationDbContext with SQL Server
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

// Cosmos DB client (shared across all Cosmos-backed services)
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var cosmosConnectionString = configuration["CosmosDbConnection"]
        ?? throw new InvalidOperationException("CosmosDbConnection not configured");
    return new CosmosClient(cosmosConnectionString, new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    });
});

// ArchivalJob repository — uses the existing ArchivalJobs container
builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var cosmosDatabaseId = configuration["CosmosDbDatabaseId"] ?? "TaskPilotArchival";
    var cosmosContainerId = configuration["CosmosDbContainerId"] ?? "ArchivalJobs";
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<ArchivalJobRepository>>();
    return new ArchivalJobRepository(cosmosClient, cosmosDatabaseId, cosmosContainerId, logger);
});
builder.Services.AddScoped<IArchivalJobRepository>(sp =>
    sp.GetRequiredService<ArchivalJobRepository>());

// Cosmos archival storage service — uses the ArchivedBoards container
builder.Services.AddScoped<ICosmosArchivalStorageService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var cosmosDatabaseId = configuration["CosmosDbDatabaseId"] ?? "TaskPilotArchival";
    var cosmosContainerId = configuration["CosmosDbArchivedBoardsContainerId"] ?? "ArchivedBoards";
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosArchivalStorageService>>();
    return new CosmosArchivalStorageService(cosmosClient, cosmosDatabaseId, cosmosContainerId, logger);
});

// Board archival service
builder.Services.AddScoped<IBoardArchivalService, BoardArchivalService>();

// Repository implementations
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();

var host = builder.Build();

// Ensure the Cosmos DB database and ArchivedBoards container exist before the host starts
// processing messages. This runs once at startup and is a no-op if already provisioned.
using (var scope = host.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();

    var databaseId = configuration["CosmosDbDatabaseId"] ?? "TaskPilotArchival";
    var archivedBoardsContainerId = configuration["CosmosDbArchivedBoardsContainerId"] ?? "ArchivedBoards";

    var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
    await databaseResponse.Database.CreateContainerIfNotExistsAsync(
        new ContainerProperties(archivedBoardsContainerId, partitionKeyPath: "/partitionKey"));
}

host.Run();
