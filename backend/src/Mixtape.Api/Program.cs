using System.Text.Json;
using System.Text.Json.Serialization;
using Mixtape.Api.Hubs;
using Mixtape.Application;
using Mixtape.Application.Rooms;
using Mixtape.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Enums skickas som text, så att fasen heter "submitting" i både hubContract.ts och nätverksfliken.
builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddSingleton<IGameNotifier, HubGameNotifier>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());
app.MapHub<GameHub>("/gamehub");

app.Run();
