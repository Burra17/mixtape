using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class SubmitTrackHandler(IGameRoomRepository rooms, IMusicCatalog catalog, IConnectionRegistry connections, TimeProvider time)
{
    // Låten hämtas från musikkällan före rummets lås, så att ingen väntar på Deezer med rummet låst. Då vet servern att låten
    // finns och går att spela, och titeln och artisten kommer från källan och inte från klienten.
    public async Task<Result<GameState>> HandleAsync(string connectionId, long trackId)
    {
        if (connections.TryGetOwner(connectionId) is not { IsHost: false } owner)
        {
            return RoomErrors.NotFound;
        }

        var playable = await catalog.GetPlayableTrackAsync(trackId);
        if (!playable.IsSuccess)
        {
            return playable.Error!;
        }

        var track = new SubmittedTrack(playable.Value.Id, playable.Value.Title, playable.Value.Artist);
        return await rooms.WithRoomAsync<GameState>(owner.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var submitted = room.SubmitTrack(owner.PlayerId!, track, now);
            return submitted.IsSuccess ? GameState.From(room, now) : submitted.Error!;
        });
    }
}
