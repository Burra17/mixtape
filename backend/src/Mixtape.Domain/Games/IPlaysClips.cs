using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games;

// Ett spel där storskärmen spelar klipp. Storskärmen hämtar länken själv, så att den aldrig finns i bilden som alla får.
public interface IPlaysClips : IGame
{
    Result<CurrentClip> GetCurrentClip();

    // Låten letas upp på id, eftersom klippet kan ha bytts medan länken hämtades.
    Result RenewLink(long trackId, string previewUrl, DateTimeOffset now);

    // Klippet gick inte att spela. Ger nästa steg om spelet hoppar över låten, eller null om klockan får gå ut som vanligt.
    Step? SkipUnplayable(long trackId);
}
