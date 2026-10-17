using Mixtape.Api.Hubs;
using Mixtape.Application;
using Mixtape.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());
app.MapHub<GameHub>("/gamehub");

app.Run();
