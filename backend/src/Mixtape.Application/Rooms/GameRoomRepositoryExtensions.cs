using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public static class GameRoomRepositoryExtensions
{
    // Koden kommer från klienten. En kod som inte går att tolka ger samma fel som ett rum som inte finns.
    public static Task<Result<T>> WithRoomAsync<T>(this IGameRoomRepository rooms, string? roomCode, Func<GameRoom, Result<T>> action) =>
        RoomCode.Parse(roomCode) is { } code ? rooms.WithRoomAsync(code, action) : Task.FromResult<Result<T>>(RoomErrors.NotFound);

    // För en kontroll utan värde, som att fasen tillåter anropet.
    public static async Task<Result> WithRoomAsync(this IGameRoomRepository rooms, string? roomCode, Func<GameRoom, Result> check) =>
        await rooms.WithRoomAsync<bool>(roomCode, room => check(room) is { Error: { } error } ? error : true);
}
