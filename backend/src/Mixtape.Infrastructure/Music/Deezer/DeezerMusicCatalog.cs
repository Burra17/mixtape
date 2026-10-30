using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;

namespace Mixtape.Infrastructure.Music.Deezer;

// Deezers öppna API, utan nyckel. Basadressen, timeouten och User-Agent sätts när HttpClient registreras.
public sealed class DeezerMusicCatalog(HttpClient http, ILogger<DeezerMusicCatalog> logger) : IMusicCatalog
{
    // Fler än som visas, så att träffar utan klipp kan filtreras bort utan ett andra anrop.
    private const int FetchLimit = 25;

    private const int PageSize = 100;

    private const int MaxPages = 10;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Söker i Deezer: {Query}", query.Value);

        var response = await GetAsync<DeezerSearchResponse>(
            $"search?q={Uri.EscapeDataString(query.Value)}&limit={FetchLimit}", MusicErrors.Unavailable, cancellationToken);
        if (!response.IsSuccess)
        {
            return response.Error!;
        }

        return (response.Value.Data ?? [])
            .Where(track => track.Readable && !string.IsNullOrEmpty(track.Preview))
            .Select(ToTrack)
            .OfType<Track>()
            .Take(IMusicCatalog.MaxResults)
            .ToList();
    }

    public async Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Hämtar klipp från Deezer: {TrackId}", trackId);

        var response = await GetAsync<DeezerTrackResponse>($"track/{trackId}", MusicErrors.TrackUnavailable, cancellationToken);
        if (!response.IsSuccess)
        {
            return response.Error!;
        }

        var track = response.Value;
        return track is
        {
            Readable: true,
            Title: { } title,
            Preview: { Length: > 0 } preview,
            Artist.Name: { } artist,
            Album: { Title: { } album, CoverXl: { } cover },
        }
            ? new PlayableTrack(track.Id, title, artist, album, cover, preview)
            : MusicErrors.TrackUnavailable;
    }

    // Deezer ger högst PageSize låtar per anrop, så listan hämtas sida för sida. MaxPages skyddar mot en lista som aldrig tar slut.
    public async Task<Result<IReadOnlyList<PlayableTrack>>> GetPlaylistTracksAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Hämtar spellista från Deezer: {PlaylistId}", playlistId);

        var tracks = new List<PlayableTrack>();
        var index = 0;
        for (var page = 0; page < MaxPages; page++)
        {
            var response = await GetAsync<DeezerPlaylistTracksResponse>(
                $"playlist/{playlistId}/tracks?limit={PageSize}&index={index}", MusicErrors.Unavailable, cancellationToken);
            if (!response.IsSuccess)
            {
                return response.Error!;
            }

            // Nästa sida börjar där den här slutade, även om Deezer gav färre låtar än PageSize.
            var data = response.Value.Data ?? [];
            tracks.AddRange(data.Select(ToPlayableTrack).OfType<PlayableTrack>());
            index += data.Count;
            if (response.Value.Next is null || data.Count == 0)
            {
                return tracks;
            }
        }

        return tracks;
    }

    private static PlayableTrack? ToPlayableTrack(DeezerTrack track) =>
        track is { Readable: true, Preview: { Length: > 0 } preview, Artist.Name: { } artist, Album: { Title: { } album, CoverXl: { } cover } }
            ? new PlayableTrack(track.Id, track.Title, artist, album, cover, preview)
            : null;

    private static Track? ToTrack(DeezerTrack track) =>
        track is { Artist.Name: { } artist, Album: { Title: { } album, CoverMedium: { } cover } }
            ? new Track(track.Id, track.Title, artist, album, cover, track.ExplicitLyrics)
            : null;

    // Bryts ut redan vid andra anropet, eftersom regeln om fel i svar med status 200 måste vara likadan för varje anrop till Deezer.
    private async Task<Result<T>> GetAsync<T>(string path, Error noData, CancellationToken cancellationToken)
        where T : class, IDeezerResponse
    {
        T? response;
        try
        {
            response = await http.GetFromJsonAsync<T>(path, JsonOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(exception, "Deezer svarade inte på {Path}", path);
            return MusicErrors.Unavailable;
        }

        if (response is null)
        {
            return MusicErrors.Unavailable;
        }

        if (response.Error is { } error)
        {
            logger.LogWarning("Deezer gav fel {Code} ({Type}: {Message}) för {Path}", error.Code, error.Type, error.Message, path);
            return error.Code switch
            {
                DeezerError.QuotaExceeded => MusicErrors.Busy,
                DeezerError.NoData => noData,
                _ => MusicErrors.Unavailable,
            };
        }

        return response;
    }
}
