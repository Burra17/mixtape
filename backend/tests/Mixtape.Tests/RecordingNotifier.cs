using Mixtape.Application.Rooms;

namespace Mixtape.Tests;

// Sparar bilderna som servern skickade av sig själv, utan att någon anropat hubben.
public sealed class RecordingNotifier : IGameNotifier
{
    public List<GameState> Sent { get; } = [];

    public Task GameStateUpdatedAsync(GameState state)
    {
        Sent.Add(state);
        return Task.CompletedTask;
    }
}
