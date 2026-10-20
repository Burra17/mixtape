namespace Mixtape.Application.Music;

// En låt redo att spelas på storskärmen. PreviewUrl gäller i 15 minuter, så den hämtas precis innan uppspelningen och cachas aldrig.
public sealed record PlayableTrack(long Id, string Title, string Artist, string Album, string CoverUrl, string PreviewUrl);
