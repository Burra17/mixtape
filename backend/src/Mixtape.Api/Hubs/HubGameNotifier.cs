using Microsoft.AspNetCore.SignalR;
using Mixtape.Application.Rooms;

namespace Mixtape.Api.Hubs;

// Skickar bilden när klockan bytt fas. Hubbens egna anrop skickar själva, eftersom de redan har en anslutning att svara på.
public sealed class HubGameNotifier(IHubContext<GameHub, IGameClient> hub) : IGameNotifier
{
    public Task GameStateUpdatedAsync(GameState state) => hub.Clients.Group(state.RoomCode).GameStateUpdated(state);
}
