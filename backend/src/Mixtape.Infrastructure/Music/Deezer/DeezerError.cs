namespace Mixtape.Infrastructure.Music.Deezer;

// Code 4 betyder enligt tredjepartskällor att för många anrop gjorts. 800 betyder att inget fanns.
internal sealed record DeezerError(string? Type, string? Message, int Code)
{
    public const int QuotaExceeded = 4;
}
