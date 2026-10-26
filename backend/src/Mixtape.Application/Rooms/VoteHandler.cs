using Mixtape.Application.Connections;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class VoteHandler(IGameRoomRepository rooms, IConnectionRegistry connections, TimeProvider time)
{
    // Spelaren känns igen via anslutningen, som vid inskicket. entry är bidragets nummer i uppspelningen.
    public async Task<Result<GameState>> HandleAsync(string connectionId, int entry)
    {
        if (connections.TryGetOwner(connectionId) is not { IsHost: false } owner)
        {
            return RoomErrors.NotFound;
        }

        return await rooms.WithRoomAsync<GameState>(owner.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var voted = room.Vote(owner.PlayerId!, entry, now);
            return voted.IsSuccess ? GameState.From(room, now) : voted.Error!;
        });
    }
}
