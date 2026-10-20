using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public interface IGameRoomRepository
{
    // Lägger till rummet om koden är ledig. Ger false om ett aktivt rum redan har koden.
    bool TryAdd(GameRoom room);

    // Tar bort rummet med dess lås, men bara om shouldRemove säger ja. Ger false om rummet saknas eller fick vara kvar.
    Task<bool> TryRemoveAsync(RoomCode code, Func<GameRoom, bool> shouldRemove);

    // Kör action med rummets lås, så att två anrop aldrig ändrar samma rum samtidigt. Ger RoomErrors.NotFound om rummet saknas.
    Task<Result<T>> WithRoomAsync<T>(RoomCode code, Func<GameRoom, Result<T>> action);
}
