using Microsoft.AspNetCore.SignalR;
using Mixtape.Application.Network;
using Mixtape.Application.Rooms;

namespace Mixtape.Api.Hubs;

public sealed class GameHub(
    CreateRoomHandler createRoom,
    ResumeRoomHandler resumeRoom,
    JoinRoomHandler joinRoom,
    LeaveRoomHandler leaveRoom,
    RemovePlayerHandler removePlayer,
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

        var result = await createRoom.HandleAsync(previous);
        if (result.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.Session.RoomCode);
            await Clients.Caller.LobbyUpdated(result.Value.Lobby);
        }

        return HubResponse<HostSession>.From(result, room => room.Session);
    }

    public async Task<HubResponse<HostSession>> ResumeRoom(HostSession session)
    {
        var result = await resumeRoom.HandleAsync(session);
        if (result.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.Session.RoomCode);
            await Clients.Caller.LobbyUpdated(result.Value.Lobby);
        }

        return HubResponse<HostSession>.From(result, room => room.Session);
    }

    public IReadOnlyList<string> GetJoinAddresses()
    {
        return joinAddresses.GetAddresses();
    }

    public async Task<HubResponse<PlayerSession>> JoinRoom(string? roomCode, string? name)
    {
        var result = await joinRoom.HandleAsync(roomCode, name);
        if (result.IsSuccess)
        {
            var (session, lobby) = result.Value;
            await Groups.AddToGroupAsync(Context.ConnectionId, session.RoomCode);
            await Groups.AddToGroupAsync(Context.ConnectionId, PlayerGroup(session.PlayerId));
            await Clients.Group(session.RoomCode).LobbyUpdated(lobby);
        }

        return HubResponse<PlayerSession>.From(result, joined => joined.Session);
    }

    // Inget svar, eftersom mobilen går tillbaka till /join oavsett utfall. Ett rum som redan är borta är inget fel för den som ville lämna.
    public async Task LeaveRoom(PlayerSession session)
    {
        var result = await leaveRoom.HandleAsync(session);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, session.RoomCode);
        if (result.IsSuccess)
        {
            var (playerId, lobby) = result.Value;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PlayerGroup(playerId));
            await Clients.Group(lobby.RoomCode).LobbyUpdated(lobby);
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

    // Spelarens egen grupp, så att servern når spelaren utan att spara ConnectionId, som byts vid varje återanslutning.
    private static string PlayerGroup(string playerId) => $"player:{playerId}";
}
