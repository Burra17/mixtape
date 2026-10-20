namespace Mixtape.Infrastructure.Music.Deezer;

internal sealed record DeezerError(string? Type, string? Message, int Code)
{
    // Enligt tredjepartskällor, inte bekräftat av Deezer.
    public const int QuotaExceeded = 4;

    // Bekräftat: en låt som inte finns.
    public const int NoData = 800;
}
