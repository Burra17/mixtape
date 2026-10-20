using Microsoft.AspNetCore.SignalR;
using Mixtape.Application.Network;
using Mixtape.Application.Rooms;

namespace Mixtape.Api.Hubs;

public sealed class GameHub(CreateRoomHandler createRoom, ResumeRoomHandler resumeRoom, IJoinAddressProvider joinAddresses) : Hub<IGameClient>
{
    // Svarar bara den som frågade, så att klienten kan se att hela vägen fram och tillbaka fungerar.
    public Task Ping()
    {
        return Clients.Caller.Pong();
    }

    // previous skickas med vid "Nytt spel", så att det gamla rummet tas bort.
    public async Task<HubResponse<HostSession>> CreateRoom(HostSession? previous)
    {
        // Anslutningen lämnar det gamla rummets grupp, så att den inte får händelser från ett rum som inte finns.
        if (previous is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, previous.RoomCode);
        }

        var result = await createRoom.HandleAsync(previous);
        if (result.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.RoomCode);
        }

        return HubResponse<HostSession>.From(result);
    }

    public async Task<HubResponse<HostSession>> ResumeRoom(HostSession session)
    {
        var result = await resumeRoom.HandleAsync(session);
        if (result.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.RoomCode);
        }

        return HubResponse<HostSession>.From(result);
    }

    public IReadOnlyList<string> GetJoinAddresses()
    {
        return joinAddresses.GetAddresses();
    }
}
