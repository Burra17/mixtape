using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public interface IGameRoomRepository
{
    // Lägger till rummet om koden är ledig. Ger false om ett aktivt rum redan har koden.
    bool TryAdd(GameRoom room);

    // Tar bort rummet med dess lås, men bara om shouldRemove säger ja. Ger false om rummet saknas eller fick vara kvar.
    Task<bool> TryRemoveAsync(RoomCode code, Func<GameRoom, bool> shouldRemove);

    // Prövar varje rum med dess lås och tar bort dem som shouldRemove säger ja till. Ger antalet borttagna.
    Task<int> RemoveWhereAsync(Func<GameRoom, bool> shouldRemove);

    // Kör action på varje rum, ett i taget och med rummets lås.
    Task ForEachRoomAsync(Action<GameRoom> action);

    // Kör action med rummets lås, så att två anrop aldrig ändrar samma rum samtidigt. Ger RoomErrors.NotFound om rummet saknas.
    Task<Result<T>> WithRoomAsync<T>(RoomCode code, Func<GameRoom, Result<T>> action);
}
