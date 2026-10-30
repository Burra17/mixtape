using Mixtape.Domain.Games.AuxCord;
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
    // Aux Cord Battles är det enda spelläget, så spelet läses som AuxCordGame. Kontraktet för fler lägen bestäms i #68 och #70.
    public static GameState From(GameRoom room, DateTimeOffset now)
    {
        var game = room.Game as AuxCordGame;
        var phase = PhaseOf(room.Stage, game);
        return new(
            room.Code.Value,
            room.Revision,
            phase,
            room.IsPaused,
            room.PauseReason,
            game?.Round ?? 0,
            room.Stage == RoomStage.InGame ? game!.Prompt : null,
            room.Stage == RoomStage.InGame && game!.SkippedRound,
            phase == GamePhase.Playing && game!.CurrentClip is { Track: var track } ? new NowPlaying(game.ClipNumber, game.Entries.Count, track.Title, track.Artist, track.Album, track.CoverUrl) : null,
            phase == GamePhase.Voting
                ? game!.Entries.Select((entry, index) => new BallotEntry(index + 1, entry.Track.Title, entry.Track.Artist, entry.Track.CoverUrl)).ToList()
                : null,
            // Vem som valde vad lämnar servern först här, när röstningen är slut.
            phase == GamePhase.Revealing
                ? game!.Entries.Select((entry, index) => new RevealedEntry(index + 1, entry.Track.Title, entry.Track.Artist, entry.Track.CoverUrl, entry.PlayerId, game.VotesFor(index + 1))).ToList()
                : null,
            phase is GamePhase.Revealing or GamePhase.GameOver
                ? room.Standings.Select(standing => new GameStanding(standing.Player.Id, standing.Player.Score, game?.RoundPointsOf(standing.Player.Id) ?? 0, standing.Place)).ToList()
                : null,
            room.RemainingTime(now) is { } remaining ? (int)Math.Ceiling(remaining.TotalMilliseconds) : null,
            SettingsState.From(room.Settings),
            ModeOptions.All,
            room.Players.Select(player => new GamePlayer(player.Id, player.Name.Value, player.ColorIndex, player.IsConnected, game?.HasSubmitted(player.Id) ?? false, game?.HasVoted(player.Id) ?? false)).ToList(),
            room.Settings.MinPlayers,
            GameRoom.MaxPlayers,
            room.CanStart);
    }

    // Kontraktets fas är rummets läge, och under spelet spelets egen fas.
    private static GamePhase PhaseOf(RoomStage stage, AuxCordGame? game) =>
        stage switch
        {
            RoomStage.Lobby => GamePhase.Lobby,
            RoomStage.GameOver => GamePhase.GameOver,
            _ => game!.Phase switch
            {
                AuxCordPhase.Submitting => GamePhase.Submitting,
                AuxCordPhase.Playing => GamePhase.Playing,
                AuxCordPhase.Voting => GamePhase.Voting,
                _ => GamePhase.Revealing,
            },
        };
}
