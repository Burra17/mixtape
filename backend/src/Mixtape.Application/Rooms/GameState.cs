using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

// Ögonblicksbilden av rummet, samma för alla, med reglernas gränser och val så att klienten inte har egna kopior. Speglas i hubContract.ts.
public sealed record GameState(
    string RoomCode,
    int Revision,
    GamePhase Phase,
    bool IsPaused,
    PauseReason? PauseReason,
    int Round,
    string? Prompt,
    bool SkippedRound,
    NowPlaying? NowPlaying,
    SongState? Song,
    SongAnswer? Answer,
    IReadOnlyList<BallotEntry>? Ballot,
    IReadOnlyList<RevealedEntry>? Reveal,
    IReadOnlyList<GameStanding>? Standings,
    int? RemainingMs,
    SettingsState Settings,
    IReadOnlyList<ModeOptions> Modes,
    IReadOnlyList<GamePlayer> Players,
    int MinPlayers,
    int MaxPlayers,
    bool CanStart)
{
    // Tiden som är kvar räknas om för varje bild, så att mobilens nedräkning inte beror på mobilens klocka.
    // Varje läge fyller sina egna fält, och de andra lägenas är null eller tomma.
    public static GameState From(GameRoom room, DateTimeOffset now)
    {
        var aux = room.Game as AuxCordGame;
        var needle = room.Game as DropTheNeedleGame;
        var phase = PhaseOf(room.Stage, room.Game);
        return new(
            room.Code.Value,
            room.Revision,
            phase,
            room.IsPaused,
            room.PauseReason,
            aux?.Round ?? 0,
            room.Stage == RoomStage.InGame ? aux?.Prompt : null,
            room.Stage == RoomStage.InGame && aux is { SkippedRound: true },
            phase == GamePhase.Playing && aux!.CurrentClip is { Track: var track } ? new NowPlaying(aux.ClipNumber, aux.Entries.Count, track.Title, track.Artist, track.Album, track.CoverUrl) : null,
            // Under gissningen bara låtens nummer, aldrig titel, artist eller omslag, eftersom bilden går att läsa i nätverksfliken.
            // Numret står kvar i avslöjandet, så att storskärmen låter klippet spela vidare.
            phase is GamePhase.Guessing or GamePhase.SongReveal ? new SongState(needle!.SongNumber, needle.SongCount, needle.CategoryId, needle.ClipNumber) : null,
            phase == GamePhase.SongReveal
                ? new SongAnswer(RevealedSong.From(needle!.LastSong!), needle.LastResults.Select(SongResultState.From).ToList())
                : null,
            phase == GamePhase.Voting
                ? aux!.Entries.Select((entry, index) => new BallotEntry(index + 1, entry.Track.Title, entry.Track.Artist, entry.Track.CoverUrl)).ToList()
                : null,
            // Vem som valde vad lämnar servern först här, när röstningen är slut.
            phase == GamePhase.Revealing
                ? aux!.Entries.Select((entry, index) => new RevealedEntry(index + 1, entry.Track.Title, entry.Track.Artist, entry.Track.CoverUrl, entry.PlayerId, aux.VotesFor(index + 1))).ToList()
                : null,
            phase is GamePhase.Revealing or GamePhase.SongReveal or GamePhase.GameOver
                ? room.Standings.Select(standing => new GameStanding(standing.Player.Id, standing.Player.Score, room.Game?.RoundPointsOf(standing.Player.Id) ?? 0, standing.Place)).ToList()
                : null,
            room.RemainingTime(now) is { } remaining ? (int)Math.Ceiling(remaining.TotalMilliseconds) : null,
            SettingsState.From(room.Settings),
            ModeOptions.All,
            room.Players.Select(player => new GamePlayer(
                player.Id,
                player.Name.Value,
                player.ColorIndex,
                player.IsConnected,
                aux?.HasSubmitted(player.Id) ?? false,
                aux?.HasVoted(player.Id) ?? false,
                needle?.HasGuessed(player.Id) ?? false)).ToList(),
            room.Settings.MinPlayers,
            GameRoom.MaxPlayers,
            room.CanStart);
    }

    // Kontraktets fas är rummets läge, och under spelet spelets egen fas.
    private static GamePhase PhaseOf(RoomStage stage, IGame? game) =>
        stage switch
        {
            RoomStage.Lobby => GamePhase.Lobby,
            RoomStage.GameOver => GamePhase.GameOver,
            _ => game switch
            {
                AuxCordGame { Phase: AuxCordPhase.Submitting } => GamePhase.Submitting,
                AuxCordGame { Phase: AuxCordPhase.Playing } => GamePhase.Playing,
                AuxCordGame { Phase: AuxCordPhase.Voting } => GamePhase.Voting,
                AuxCordGame => GamePhase.Revealing,
                DropTheNeedleGame { Phase: DropTheNeedlePhase.Guessing } => GamePhase.Guessing,
                DropTheNeedleGame => GamePhase.SongReveal,
                _ => throw new InvalidOperationException($"Spelet {game?.GetType().Name} saknar faser i kontraktet."),
            },
        };
}
