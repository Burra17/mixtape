using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games;

namespace Mixtape.Application.Rooms;

public sealed class GetClipHandler(IGameRoomRepository rooms, IMusicCatalog catalog, IGameNotifier notifier, TimeProvider time)
{
    // En länk som är för gammal förnyas utanför rummets lås, så att ingen väntar på Deezer med rummet låst.
    public async Task<Result<Clip>> HandleAsync(HostSession host)
    {
        var current = await rooms.WithRoomAsync(host.RoomCode, room => room.WithGameForHost<IPlaysClips, CurrentClip>(host.HostToken, game => game.GetCurrentClip()));
        if (!current.IsSuccess)
        {
            return current.Error!;
        }

        var clip = current.Value;
        if (!clip.NeedsNewLink(time.GetUtcNow()))
        {
            return new Clip(clip.Number, clip.Track.PreviewUrl);
        }

        var playable = await catalog.GetPlayableTrackAsync(clip.Track.Id);
        if (playable.Error == MusicErrors.TrackUnavailable)
        {
            await SkipAsync(host, clip.Track.Id);
            return playable.Error;
        }

        if (!playable.IsSuccess)
        {
            return playable.Error!;
        }

        var renewed = await rooms.WithRoomAsync(host.RoomCode, room => room.WithGame<IPlaysClips>(game => game.RefreshTrack(clip.Track.Id, playable.Value.PreviewUrl, playable.Value.Contributors, time.GetUtcNow())));
        return renewed.IsSuccess ? new Clip(clip.Number, playable.Value.PreviewUrl) : renewed.Error!;
    }

    // Ett spel som hoppar över låten byter klipp utan att någon spelare anropat hubben, så bilden skickas här.
    private async Task SkipAsync(HostSession host, long trackId)
    {
        var skipped = await rooms.WithRoomAsync<(int Revision, GameState State)>(host.RoomCode, room =>
        {
            var revision = room.Revision;
            var result = room.SkipUnplayableClip(host.HostToken, trackId, time.GetUtcNow());
            return result.IsSuccess ? (revision, GameState.From(room, time.GetUtcNow())) : result.Error!;
        });

        if (skipped.IsSuccess && skipped.Value.State.Revision != skipped.Value.Revision)
        {
            await notifier.GameStateUpdatedAsync(skipped.Value.State);
        }
    }
}
