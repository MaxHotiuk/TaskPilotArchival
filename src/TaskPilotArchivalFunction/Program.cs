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

    

builder.Services.Configure<Infrastructure.Configuration.BlobStorageOptions>(
    builder.Configuration.GetSection("BlobStorage"));

// Register services and repositories using interfaces
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IBoardArchivalService, BoardArchivalService>();

// Register repository implementations
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();

// Cosmos DB configuration
builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var cosmosConnectionString = configuration["CosmosDbConnection"] ?? throw new InvalidOperationException("CosmosDbConnection not configured");
    return new CosmosClient(cosmosConnectionString);
});
builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var cosmosDatabaseId = configuration["CosmosDbDatabaseId"] ?? "TaskPilotArchival";
    var cosmosContainerId = configuration["CosmosDbContainerId"] ?? "ArchivalJobs";
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<IArchivalJobRepository>>();
    return new ArchivalJobRepository(cosmosClient, cosmosDatabaseId, cosmosContainerId, (ILogger<ArchivalJobRepository>)logger);
});

// Register repository interface
builder.Services.AddScoped<IArchivalJobRepository>(sp =>
    sp.GetRequiredService<ArchivalJobRepository>());

builder.Build().Run();
