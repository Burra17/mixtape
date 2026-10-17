using Microsoft.AspNetCore.SignalR;

namespace Mixtape.Api.Hubs;

public sealed class GameHub : Hub<IGameClient>
{
    // Svarar bara den som frågade, så att klienten kan se att hela vägen fram och tillbaka fungerar.
    public Task Ping()
    {
        return Clients.Caller.Pong();
    }
}
