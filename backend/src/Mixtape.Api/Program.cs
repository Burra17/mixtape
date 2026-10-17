using Mixtape.Application;
using Mixtape.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.Run();
