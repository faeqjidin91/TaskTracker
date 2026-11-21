using TaskTracker.Api.Extensions;
using TaskTracker.Application;
using TaskTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSeedSettingsFromEnvFile(builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.UseApiPipeline();

await app.RunAsync();
