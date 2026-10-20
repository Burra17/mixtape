namespace Mixtape.Application.Rooms;

public sealed class CleanUpRoomsHandler(IGameRoomRepository rooms, TimeProvider time)
{
    // Tar bort rum som stått tomma för länge och svarar med hur många. Ingen är ansluten till dem, så det finns ingen att berätta det för.
    public Task<int> HandleAsync()
    {
        var now = time.GetUtcNow();
        return rooms.RemoveWhereAsync(room => room.IsAbandoned(now));
    }
}
