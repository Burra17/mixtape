using Microsoft.AspNetCore.SignalR;
using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Application.Network;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games;

namespace Mixtape.Api.Hubs;

public sealed class GameHub(
    CreateRoomHandler createRoom,
    CheckRoomHandler checkRoom,
    ResumeRoomHandler resumeRoom,
    JoinRoomHandler joinRoom,
    RejoinRoomHandler rejoinRoom,
    LeaveRoomHandler leaveRoom,
    RemovePlayerHandler removePlayer,
    ChooseModeHandler chooseMode,
    UpdateAuxCordSettingsHandler updateAuxCordSettings,
    UpdateDropTheNeedleSettingsHandler updateDropTheNeedleSettings,
    StartGameHandler startGame,
    DisconnectHandler disconnect,
    SearchTracksHandler searchTracks,
    GetCategoriesHandler getCategories,
    SubmitTrackHandler submitTrack,
    GetClipHandler getClip,
    VoteHandler vote,
    GuessHandler guess,
    GetBallotHandler getBallot,
    PlayAgainHandler playAgain,
    PauseGameHandler pauseGame,
    ContinueGameHandler continueGame,
    EndGameHandler endGame,
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
            var (session, state, closedRoomCode) = result.Value;
            if (closedRoomCode is not null)
            {
                await Clients.Group(closedRoomCode).RoomClosed();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, session.RoomCode);
            await Clients.Caller.GameStateUpdated(state);
        }

        return HubResponse<HostSession>.From(result, room => room.Session);
    }

    // Storskärmen frågar när sidan laddas om det sparade rummet finns kvar, utan att ta tillbaka det.
    public async Task<HubResponse<HostSession>> CheckRoom(HostSession session)
    {
        return HubResponse<HostSession>.From(await checkRoom.HandleAsync(session));
    }

    // Häver pausen, så bilden går till hela rummet och inte bara till storskärmen.
    public async Task<HubResponse<HostSession>> ResumeRoom(HostSession session)
    {
        var result = await resumeRoom.HandleAsync(session, Context.ConnectionId);
        if (result.IsSuccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, result.Value.Session.RoomCode);
            await SendToRoomAsync(result.Value.State);
        }

        return HubResponse<HostSession>.From(result, room => room.Session);
    }

    public IReadOnlyList<string> GetJoinAddresses()
    {
        return joinAddresses.GetAddresses();
    }

    // Bara spelare i ett rum får söka. Handlern slår upp anslutningen, så klienten behöver inte skicka med sin token.
    public async Task<HubResponse<IReadOnlyList<Track>>> Search(string? query)
    {
        return HubResponse<IReadOnlyList<Track>>.From(await searchTracks.HandleAsync(Context.ConnectionId, query));
    }

    // Spelaren känns igen via anslutningen, som vid sökningen. Bilden går till hela rummet, så att storskärmen ser vem som är klar.
    public async Task<HubResponse<GameState>> SubmitTrack(long trackId)
    {
        return await SendToRoomAsync(await submitTrack.HandleAsync(Context.ConnectionId, trackId));
    }

    // Bara storskärmen får länken till klippet. Den hämtar den själv för varje nytt klipp och efter en omladdning.
    public async Task<HubResponse<Clip>> GetClip(HostSession host)
    {
        return HubResponse<Clip>.From(await getClip.HandleAsync(host));
    }

    // Spelaren känns igen via anslutningen. Bilden går till hela rummet, så att storskärmen ser vem som röstat.
    public async Task<HubResponse<GameState>> Vote(int entry)
    {
        return await SendToRoomAsync(await vote.HandleAsync(Context.ConnectionId, entry));
    }

    // Spelaren känns igen via anslutningen, som vid inskicket. Bilden går till hela rummet, så att storskärmen bockar av spelaren.
    public async Task<HubResponse<GameState>> Guess(long trackId)
    {
        return await SendToRoomAsync(await guess.HandleAsync(Context.ConnectionId, trackId));
    }

    // Bara till spelaren själv: det egna bidraget och den egna rösten. Mobilen hämtar dem när röstningen börjar och efter en återanslutning.
    public async Task<HubResponse<Ballot>> GetBallot()
    {
        return HubResponse<Ballot>.From(await getBallot.HandleAsync(Context.ConnectionId));
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
            await SendToRoomAsync(result.Value.State);
        }
    }

    // Inget svar: en spelare som redan är borta syns ändå inte i listan, och storskärmen får den nya listan med GameStateUpdated.
    public async Task RemovePlayer(HostSession host, string? playerId)
    {
        var result = await removePlayer.HandleAsync(host, playerId);
        if (result.IsSuccess)
        {
            var (removedId, state) = result.Value;
            await Clients.Group(PlayerGroup(removedId)).RemovedFromRoom();
            await SendToRoomAsync(state);
        }
    }

    // Klienten läser bara felet, och bilden går till hela rummet eftersom mobilerna också visar läget och inställningarna.
    public async Task<HubResponse<GameState>> ChooseMode(HostSession host, GameMode mode)
    {
        return await SendToRoomAsync(await chooseMode.HandleAsync(host, mode));
    }

    public async Task<HubResponse<GameState>> UpdateAuxCordSettings(HostSession host, int? rounds, int? clipSeconds, int? submittingSeconds)
    {
        return await SendToRoomAsync(await updateAuxCordSettings.HandleAsync(host, rounds, clipSeconds, submittingSeconds));
    }

    public async Task<HubResponse<GameState>> UpdateDropTheNeedleSettings(HostSession host, int? songs, int? clipSeconds, int? thinkingSeconds, string? categoryId)
    {
        return await SendToRoomAsync(await updateDropTheNeedleSettings.HandleAsync(host, songs, clipSeconds, thinkingSeconds, categoryId));
    }

    // Storskärmen och mobilerna hämtar kategorierna själva, eftersom de inte ändras och därför inte behöver finnas i varje bild.
    public IReadOnlyList<CategoryOption> GetCategories()
    {
        return getCategories.Handle();
    }

    public async Task<HubResponse<GameState>> StartGame(HostSession host)
    {
        return await SendToRoomAsync(await startGame.HandleAsync(host));
    }

    // Bilden av lobbyn går till hela rummet, så att mobilerna lämnar slutställningen samtidigt som storskärmen.
    public async Task<HubResponse<GameState>> PlayAgain(HostSession host)
    {
        return await SendToRoomAsync(await playAgain.HandleAsync(host));
    }

    // Pausen gäller hela rummet, så bilden går till alla och mobilerna kan visa att spelet är pausat.
    public async Task<HubResponse<GameState>> PauseGame(HostSession host)
    {
        return await SendToRoomAsync(await pauseGame.HandleAsync(host));
    }

    public async Task<HubResponse<GameState>> ContinueGame(HostSession host)
    {
        return await SendToRoomAsync(await continueGame.HandleAsync(host));
    }

    // Slutställningen går till hela rummet, så att mobilerna lämnar fasen samtidigt som storskärmen.
    public async Task<HubResponse<GameState>> EndGame(HostSession host)
    {
        return await SendToRoomAsync(await endGame.HandleAsync(host));
    }

    // Spelaren eller storskärmen markeras som borta men behåller sin plats. En borttagen spelare eller ett rum som inte finns längre ger fel, och då finns inget att berätta.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (connections.Unregister(Context.ConnectionId) is { } owner)
        {
            var result = await disconnect.HandleAsync(owner);
            if (result.IsSuccess)
            {
                await SendToRoomAsync(result.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task EnterRoomAsync(JoinedRoom joined)
    {
        var (session, state) = joined;
        await Groups.AddToGroupAsync(Context.ConnectionId, session.RoomCode);
        await Groups.AddToGroupAsync(Context.ConnectionId, PlayerGroup(session.PlayerId));
        await SendToRoomAsync(state);
    }

    private async Task<HubResponse<GameState>> SendToRoomAsync(Result<GameState> result)
    {
        if (result.IsSuccess)
        {
            await SendToRoomAsync(result.Value);
        }

        return HubResponse<GameState>.From(result);
    }

    private Task SendToRoomAsync(GameState state) => Clients.Group(state.RoomCode).GameStateUpdated(state);

    // Spelarens egen grupp, så att servern når spelaren utan att spara ConnectionId, som byts vid varje återanslutning.
    private static string PlayerGroup(string playerId) => $"player:{playerId}";
}
