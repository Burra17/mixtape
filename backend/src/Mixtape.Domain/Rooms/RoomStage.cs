namespace Mixtape.Domain.Rooms;

// Rummets läge. Under spelet har spelläget egna faser, som rummet inte känner till.
public enum RoomStage
{
    Lobby,
    InGame,
    GameOver,
}
