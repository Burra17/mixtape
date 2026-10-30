using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class SubmitTrackHandler(IGameRoomRepository rooms, IMusicCatalog catalog, IConnectionRegistry connections, TimeProvider time)
{
    // Låten hämtas från musikkällan utanför rummets lås, så att servern vet att den går att spela utan att låsa rummet under tiden.
    public async Task<Result<GameState>> HandleAsync(string connectionId, long trackId)
    {
        if (connections.TryGetOwner(connectionId) is not { IsHost: false } owner)
        {
            return RoomErrors.NotFound;
        }

        // Fasen kontrolleras före Deezer, så att ett sent inskick får fasfelet och inte ett fel från musikkällan.
        var allowed = await rooms.WithRoomAsync(owner.RoomCode, room => room.WithGame<AuxCordGame>(game => game.CanSearch()));
        if (!allowed.IsSuccess)
        {
            return allowed.Error!;
        }

        var playable = await catalog.GetPlayableTrackAsync(trackId);
        if (!playable.IsSuccess)
        {
            return playable.Error!;
        }

        var track = playable.Value.ToSubmittedTrack();
        return await rooms.WithRoomAsync<GameState>(owner.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var submitted = room.Act<AuxCordGame>(owner.PlayerId!, now, game => game.SubmitTrack(owner.PlayerId!, track, now));
            return submitted.IsSuccess ? GameState.From(room, now) : submitted.Error!;
        });
    }
}
