using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games;

// Ett spel där spelarna söker låtar i mobilen, för att skicka in eller gissa.
public interface ISearchesTracks : IGame
{
    Result CanSearch();
}
