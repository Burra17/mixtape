using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;

namespace Mixtape.Infrastructure.Music.Deezer;

// Deezers öppna API, utan nyckel. Basadressen, timeouten och User-Agent sätts när HttpClient registreras.
public sealed class DeezerMusicCatalog(HttpClient http, ILogger<DeezerMusicCatalog> logger) : IMusicCatalog
{
    public const int MaxResults = 8;

    // Fler än som visas, så att träffar utan klipp kan filtreras bort utan ett andra anrop.
    private const int FetchLimit = 25;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Söker i Deezer: {Query}", query.Value);

        DeezerSearchResponse? response;
        try
        {
            response = await http.GetFromJsonAsync<DeezerSearchResponse>(
                $"search?q={Uri.EscapeDataString(query.Value)}&limit={FetchLimit}", JsonOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(exception, "Deezer svarade inte på sökningen {Query}", query.Value);
            return MusicErrors.Unavailable;
        }

        if (response?.Error is { } error)
        {
            logger.LogWarning("Deezer gav fel {Code} ({Type}: {Message}) för {Query}", error.Code, error.Type, error.Message, query.Value);
            return error.Code == DeezerError.QuotaExceeded ? MusicErrors.Busy : MusicErrors.Unavailable;
        }

        return (response?.Data ?? [])
            .Where(track => track.Readable && !string.IsNullOrEmpty(track.Preview))
            .Take(MaxResults)
            .Select(track => new Track(track.Id, track.Title, track.Artist.Name, track.Album.Title, track.Album.CoverMedium, track.ExplicitLyrics))
            .ToList();
    }
}
