namespace Mixtape.Application.Rooms;

public sealed class AdvancePhasesHandler(IGameRoomRepository rooms, IGameNotifier notifier, TimeProvider time)
{
    // Låter varje rum byta fas när deadline passerat. Bilderna skickas efter att låsen släppts, så att ingen väntar på nätet med ett rum låst.
    public async Task HandleAsync()
    {
        var now = time.GetUtcNow();
        var changed = new List<GameState>();
        await rooms.ForEachRoomAsync(room =>
        {
            if (room.Tick(now))
            {
                changed.Add(GameState.From(room, now));
            }
        });

        foreach (var state in changed)
        {
            await notifier.GameStateUpdatedAsync(state);
        }
    }
}
