using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class StartGameHandler(IGameRoomRepository rooms, IPromptProvider prompts, TimeProvider time)
{
    // Promptarna dras med rummets lås, så att de använda promptarna inte hinner ändras emellan.
    public Task<Result<GameState>> HandleAsync(HostSession host) =>
        rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var started = room.StartGame(host.HostToken, prompts.Draw(room.Settings.Rounds, room.UsedPrompts), now);
            return started.IsSuccess ? GameState.From(room, now) : started.Error!;
        });
}
