using Mixtape.Domain.Common;
using Mixtape.Domain.Games;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class StartGameHandler(IGameRoomRepository rooms, IPromptProvider prompts, TimeProvider time)
{
    // Promptarna dras med rummets lås, så att de använda promptarna inte hinner ändras emellan.
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var started = room.StartGame(host.HostToken, settings => CreateGame(room, settings), now);
            return started.IsSuccess ? GameState.From(room, now) : started.Error!;
        });

    // Det enda stället som kopplar ett läge till sitt spel.
    private Result<IGame> CreateGame(GameRoom room, IGameSettings settings) =>
        settings switch
        {
            AuxCordSettings aux => Result<IGame>.Success(new AuxCordGame(prompts.Draw(aux.Rounds, room.UsedContent<AuxCordGame>()), aux, Random.Shared)),
            _ => RoomErrors.ModeNotReady,
        };
}
