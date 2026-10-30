using Mixtape.Domain.Music;

namespace Mixtape.Application.Music;

// En låt redo för storskärmen. PreviewUrl gäller i 15 minuter, så den hämtas vid inskicket, förnyas efter en lång paus och cachas aldrig.
public sealed record PlayableTrack(long Id, string Title, string Artist, string Album, string CoverUrl, string PreviewUrl)
{
    public SubmittedTrack ToSubmittedTrack() => new(Id, Title, Artist, Album, CoverUrl, PreviewUrl);
}
