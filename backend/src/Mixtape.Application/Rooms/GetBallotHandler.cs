using Mixtape.Application.Connections;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class GetBallotHandler(IGameRoomRepository rooms, IConnectionRegistry connections)
{
    // Spelarens egen del av omröstningen går bara till spelaren, eftersom det egna bidraget avslöjar vem som valde låten.
    public async Task<Result<Ballot>> HandleAsync(string connectionId)
    {
        if (connections.TryGetOwner(connectionId) is not { IsHost: false } owner)
        {
            return RoomErrors.NotFound;
        }

        return await rooms.WithRoomAsync<Ballot>(owner.RoomCode, room =>
        {
            var allowed = room.CanVote();
            return allowed.IsSuccess ? new Ballot(room.EntryNumberOf(owner.PlayerId!), room.VoteOf(owner.PlayerId!)) : allowed.Error!;
        });
    }
}
