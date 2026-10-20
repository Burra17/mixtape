using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public interface IGameRoomRepository
{
    // Lägger till rummet om koden är ledig. Ger false om ett aktivt rum redan har koden.
    bool TryAdd(GameRoom room);

    void Remove(RoomCode code);

    // Kör action med rummets lås, så att två anrop aldrig ändrar samma rum samtidigt. Ger RoomErrors.NotFound om rummet saknas.
    Task<Result<T>> WithRoomAsync<T>(RoomCode code, Func<GameRoom, Result<T>> action);
}
