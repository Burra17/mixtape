using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class GetClipHandler(IGameRoomRepository rooms, IMusicCatalog catalog, TimeProvider time)
{
    // En länk som är för gammal efter en lång paus förnyas utanför rummets lås, så att ingen väntar på Deezer med rummet låst.
    public async Task<Result<Clip>> HandleAsync(HostSession host)
    {
        var current = await rooms.WithRoomAsync<(int Index, Entry Entry)>(host.RoomCode, room =>
        {
            var clip = room.CurrentClipFor(host.HostToken);
            return clip.IsSuccess ? (room.ClipNumber, clip.Value) : clip.Error!;
        });
        if (!current.IsSuccess)
        {
            return current.Error!;
        }

        var (index, entry) = current.Value;
        if (!entry.NeedsNewLink(time.GetUtcNow()))
        {
            return new Clip(index, entry.Track.PreviewUrl);
        }

        var playable = await catalog.GetPlayableTrackAsync(entry.Track.Id);
        if (!playable.IsSuccess)
        {
            return playable.Error!;
        }

        var renewed = await rooms.WithRoomAsync(host.RoomCode, room => room.RenewLink(entry.Track.Id, playable.Value.PreviewUrl, time.GetUtcNow()));
        return renewed.IsSuccess ? new Clip(index, playable.Value.PreviewUrl) : renewed.Error!;
    }
}
