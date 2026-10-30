using Mixtape.Application.Connections;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games.AuxCord;
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

        return await rooms.WithRoomAsync<Ballot>(owner.RoomCode, room => room.WithGame<AuxCordGame, Ballot>(game =>
        {
            var allowed = game.CanVote();
            return allowed.IsSuccess ? new Ballot(game.EntryNumberOf(owner.PlayerId!), game.VoteOf(owner.PlayerId!)) : allowed.Error!;
        }));
    }
}
