using Microsoft.EntityFrameworkCore;
using NeoWatch.Infrastructure;
using NeoWatch.Worker;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(logging => logging
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    // HttpClient logs request URLs. Ours contain the API key, so keep these quiet.
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .WriteTo.Console());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddNeoWatchInfrastructure(builder.Configuration);
builder.Services.AddScoped<IngestionPipeline>();
builder.Services.AddHostedService<IngestionWorker>();

var host = builder.Build();

// Apply any pending EF Core migrations before the worker starts.
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NeoWatchDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
