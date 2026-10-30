using Mixtape.Domain.Common;
using Mixtape.Domain.Games.AuxCord;

namespace Mixtape.Application.Rooms;

public sealed class StartGameHandler(IGameRoomRepository rooms, IPromptProvider prompts, TimeProvider time)
{
    // Promptarna dras med rummets lås, så att de använda promptarna inte hinner ändras emellan.
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var game = new AuxCordGame(prompts.Draw(room.Settings.Rounds, room.UsedContent<AuxCordGame>()), room.Settings, Random.Shared);
            var started = room.StartGame(host.HostToken, game, now);
            return started.IsSuccess ? GameState.From(room, now) : started.Error!;
        });
}
