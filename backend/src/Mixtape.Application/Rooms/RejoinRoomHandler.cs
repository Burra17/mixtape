using Mixtape.Application.Connections;
using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class RejoinRoomHandler(IGameRoomRepository rooms, IConnectionRegistry connections)
{
    // Spelaren känns igen på sin token. Den nya anslutningen kopplas med rummets lås, så att en sen disconnect från den gamla ser den.
    public Task<Result<JoinedRoom>> HandleAsync(PlayerSession session, string connectionId) =>
        rooms.WithRoomAsync<JoinedRoom>(session.RoomCode, room =>
        {
            var rejoined = room.Rejoin(session.PlayerToken);
            if (!rejoined.IsSuccess)
            {
                return rejoined.Error!;
            }

            var player = rejoined.Value;
            connections.Register(connectionId, ConnectionOwner.Player(room.Code.Value, player.Id));
            return new JoinedRoom(new PlayerSession(room.Code.Value, player.Id, player.Token), LobbyState.From(room));
        });
}
