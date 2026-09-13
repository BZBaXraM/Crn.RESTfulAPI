using API.Extensions;
using Application;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();

await app.MigrateDatabaseAsync();
await app.SeedDatabaseAsync();

app.Run();

// Exposed so API.Tests can use WebApplicationFactory<Program>.
public partial class Program;
