using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class StartGameHandler(IGameRoomRepository rooms, IPromptProvider prompts, CategorySongs categorySongs, TimeProvider time)
{
    // Drop the Needles låtar hämtas från Deezer utanför rummets lås, så att ingen väntar på Deezer med rummet låst.
    public async Task<Result<GameState>> HandleAsync(HostSession host)
    {
        var settings = await rooms.WithRoomAsync(host.RoomCode, room => room.IsHost(host.HostToken) ? Result<IGameSettings>.Success(room.Settings) : RoomErrors.NotFound);
        if (!settings.IsSuccess)
        {
            return settings.Error!;
        }

        Songs? songs = null;
        if (settings.Value is DropTheNeedleSettings needle)
        {
            var loaded = await categorySongs.LoadAsync(needle.CategoryId);
            if (!loaded.IsSuccess)
            {
                return loaded.Error!;
            }

            songs = new Songs(needle.CategoryId, loaded.Value);
        }

        // Promptarna och låtarna lottas med rummets lås, så att historiken inte hinner ändras emellan.
        return await rooms.WithRoomAsync<GameState>(host.RoomCode, room =>
        {
            var now = time.GetUtcNow();
            var started = room.StartGame(host.HostToken, current => CreateGame(room, current, songs), now);
            return started.IsSuccess ? GameState.From(room, now) : started.Error!;
        });
    }

    // Det enda stället som kopplar ett läge till sitt spel.
    private Result<IGame> CreateGame(GameRoom room, IGameSettings settings, Songs? songs) =>
        settings switch
        {
            AuxCordSettings aux => Result<IGame>.Success(new AuxCordGame(prompts.Draw(aux.Rounds, room.UsedContent<AuxCordGame>()), aux, Random.Shared)),
            DropTheNeedleSettings needle when songs?.CategoryId == needle.CategoryId => DrawSongs(room, needle, songs.Pool),
            _ => GameModeErrors.SettingsChanged,
        };

    private static Result<IGame> DrawSongs(GameRoom room, DropTheNeedleSettings settings, IReadOnlyList<SubmittedTrack> pool)
    {
        var drawn = SongDraw.Draw(pool, settings.Songs, room.UsedContent<DropTheNeedleGame>(), Random.Shared);
        return drawn.IsSuccess ? Result<IGame>.Success(new DropTheNeedleGame(drawn.Value, settings)) : drawn.Error!;
    }

    // Kategorins låtar, och vilken kategori de hämtades för.
    private sealed record Songs(string CategoryId, IReadOnlyList<SubmittedTrack> Pool);
}
