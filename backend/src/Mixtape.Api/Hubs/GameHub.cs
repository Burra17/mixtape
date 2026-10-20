using Microsoft.AspNetCore.SignalR;
using Mixtape.Application.Connections;
using Mixtape.Application.Network;
using Mixtape.Application.Rooms;

namespace Mixtape.Api.Hubs;

public sealed class GameHub(
    CreateRoomHandler createRoom,
    CheckRoomHandler checkRoom,
    ResumeRoomHandler resumeRoom,
    JoinRoomHandler joinRoom,
    RejoinRoomHandler rejoinRoom,
    LeaveRoomHandler leaveRoom,
    RemovePlayerHandler removePlayer,
    DisconnectHandler disconnect,
    IConnectionRegistry connections,
    IJoinAddressProvider joinAddresses) : Hub<IGameClient>
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

        var result = await createRoom.HandleAsync(previous, Context.ConnectionId);
        if (result.IsSuccess)
        {
            var (session, lobby, closedRoomCode) = result.Value;
            if (closedRoomCode is not null)
            {
                await Clients.Group(closedRoomCode).RoomClosed();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, session.RoomCode);
            await Clients.Caller.LobbyUpdated(lobby);
        }

        return HubResponse<HostSession>.From(result, room => room.Session);
    }

    // Storskärmen frågar när sidan laddas om det sparade rummet finns kvar, utan att ta tillbaka det.
    public async Task<HubResponse<HostSession>> CheckRoom(HostSession session)
    {
        return HubResponse<HostSession>.From(await checkRoom.HandleAsync(session));
    }

    // Häver pausen, så lobbyn går till hela rummet och inte bara till storskärmen.
    public async Task<HubResponse<HostSession>> ResumeRoom(HostSession session)
    {
        var result = await resumeRoom.HandleAsync(session, Context.ConnectionId);
        if (result.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.Session.RoomCode);
            await Clients.Group(result.Value.Session.RoomCode).LobbyUpdated(result.Value.Lobby);
        }

        return HubResponse<HostSession>.From(result, room => room.Session);
    }

    public IReadOnlyList<string> GetJoinAddresses()
    {
        return joinAddresses.GetAddresses();
    }

    public async Task<HubResponse<PlayerSession>> JoinRoom(string? roomCode, string? name)
    {
        var result = await joinRoom.HandleAsync(roomCode, name, Context.ConnectionId);
        if (result.IsSuccess)
        {
            await EnterRoomAsync(result.Value);
        }

        return HubResponse<PlayerSession>.From(result, joined => joined.Session);
    }

    // Mobilen kommer tillbaka med sin token efter en låst skärm eller en omladdning och får samma plats igen.
    public async Task<HubResponse<PlayerSession>> RejoinRoom(PlayerSession session)
    {
        var result = await rejoinRoom.HandleAsync(session, Context.ConnectionId);
        if (result.IsSuccess)
        {
            await EnterRoomAsync(result.Value);
        }

        return HubResponse<PlayerSession>.From(result, joined => joined.Session);
    }

    // Inget svar, eftersom mobilen går tillbaka till /join oavsett utfall. En borttagen mobil anropar också LeaveRoom, och då misslyckas Leave men grupperna lämnas ändå.
    public async Task LeaveRoom(PlayerSession session)
    {
        var result = await leaveRoom.HandleAsync(session);
        connections.Unregister(Context.ConnectionId);

        // Klientens egna värden räcker här, eftersom det bara är den egna anslutningen som lämnar grupperna.
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, session.RoomCode.ToUpperInvariant());
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, PlayerGroup(session.PlayerId));
        if (result.IsSuccess)
        {
            await Clients.Group(result.Value.Lobby.RoomCode).LobbyUpdated(result.Value.Lobby);
        }
    }

    // Inget svar: en spelare som redan är borta syns ändå inte i listan, och storskärmen får den nya listan med LobbyUpdated.
    public async Task RemovePlayer(HostSession host, string? playerId)
    {
        var result = await removePlayer.HandleAsync(host, playerId);
        if (result.IsSuccess)
        {
            var (removedId, lobby) = result.Value;
            await Clients.Group(PlayerGroup(removedId)).RemovedFromRoom();
            await Clients.Group(lobby.RoomCode).LobbyUpdated(lobby);
        }
    }

    // Spelaren eller storskärmen markeras som borta men behåller sin plats. En borttagen spelare eller ett rum som inte finns längre ger fel, och då finns inget att berätta.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (connections.Unregister(Context.ConnectionId) is { } owner)
        {
            var result = await disconnect.HandleAsync(owner);
            if (result.IsSuccess)
            {
                await Clients.Group(result.Value.RoomCode).LobbyUpdated(result.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task EnterRoomAsync(JoinedRoom joined)
    {
        var (session, lobby) = joined;
        await Groups.AddToGroupAsync(Context.ConnectionId, session.RoomCode);
        await Groups.AddToGroupAsync(Context.ConnectionId, PlayerGroup(session.PlayerId));
        await Clients.Group(session.RoomCode).LobbyUpdated(lobby);
    }

    // Spelarens egen grupp, så att servern når spelaren utan att spara ConnectionId, som byts vid varje återanslutning.
    private static string PlayerGroup(string playerId) => $"player:{playerId}";
}
