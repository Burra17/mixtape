using System.Collections.Concurrent;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Infrastructure.Rooms;

// Ett spel lever en kväll, så rummen ligger i minnet. Registreras som singleton.
public sealed class InMemoryGameRoomRepository : IGameRoomRepository
{
    private readonly ConcurrentDictionary<string, Entry> _rooms = new();

    public bool TryAdd(GameRoom room) => _rooms.TryAdd(room.Code.Value, new Entry(room));

    public async Task<bool> TryRemoveAsync(RoomCode code, Func<GameRoom, bool> shouldRemove)
    {
        if (!_rooms.TryGetValue(code.Value, out var entry))
        {
            return false;
        }

        await entry.Gate.WaitAsync();
        try
        {
            if (entry.Removed || !shouldRemove(entry.Room) || !_rooms.TryRemove(code.Value, out _))
            {
                return false;
            }

            entry.Removed = true;
            return true;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    public async Task<int> RemoveWhereAsync(Func<GameRoom, bool> shouldRemove)
    {
        var removed = 0;
        foreach (var entry in _rooms.Values)
        {
            if (await TryRemoveAsync(entry.Room.Code, shouldRemove))
            {
                removed++;
            }
        }

        return removed;
    }

    public async Task<Result<T>> WithRoomAsync<T>(RoomCode code, Func<GameRoom, Result<T>> action)
    {
        if (!_rooms.TryGetValue(code.Value, out var entry))
        {
            return RoomErrors.NotFound;
        }

        await entry.Gate.WaitAsync();
        try
        {
            return entry.Removed ? RoomErrors.NotFound : action(entry.Room);
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    // ConcurrentDictionary skyddar listan med rum. Låset skyddar varje rum, så att två spelare som trycker samtidigt inte ändrar det i otakt.
    private sealed record Entry(GameRoom Room)
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        // Sätts med låset när rummet tas bort. Ett anrop som redan väntade på låset ska då få NotFound, inte ändra ett rum som inte finns längre.
        public bool Removed { get; set; }
    }
}
