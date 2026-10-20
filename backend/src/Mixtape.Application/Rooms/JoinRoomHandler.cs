using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class JoinRoomHandler(IGameRoomRepository rooms)
{
    public async Task<Result<JoinedRoom>> HandleAsync(string? roomCode, string? name)
    {
        if (RoomCode.Parse(roomCode) is not { } code)
        {
            return RoomErrors.NotFound;
        }

        return await rooms.WithRoomAsync<JoinedRoom>(code, room =>
        {
            var joined = room.Join(name);
            if (!joined.IsSuccess)
            {
                return joined.Error!;
            }

            var player = joined.Value;
            return new JoinedRoom(new PlayerSession(room.Code.Value, player.Id, player.Token), LobbyState.From(room));
        });
    }
}
