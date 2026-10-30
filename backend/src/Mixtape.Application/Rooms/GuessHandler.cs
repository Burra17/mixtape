using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class GuessHandler(IGameRoomRepository rooms, IMusicCatalog catalog, IConnectionRegistry connections, TimeProvider time)
{
    // Tiden tas när gissningen kommer fram, före Deezer, så att snabbast inte beror på Deezers svarstid.
    // Låten hämtas utanför rummets lås, som vid inskicket, och gissningen gäller låten som spelades när den kom fram.
    public async Task<Result<GameState>> HandleAsync(string connectionId, long trackId)
    {
        var receivedAt = time.GetUtcNow();
        if (connections.TryGetOwner(connectionId) is not { IsHost: false } owner)
        {
            return RoomErrors.NotFound;
        }

        var song = await rooms.WithRoomAsync(owner.RoomCode, room => room.WithGame<DropTheNeedleGame, int>(game => game.CanGuess(owner.PlayerId!)));
        if (!song.IsSuccess)
        {
            return song.Error!;
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
            var guessed = room.Act<DropTheNeedleGame>(owner.PlayerId!, now, game => game.Guess(owner.PlayerId!, song.Value, track, receivedAt));
            return guessed.IsSuccess ? GameState.From(room, now) : guessed.Error!;
        });
    }
}
