using System.Runtime.CompilerServices;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

// Domäntesterna för Aux Cord Battles skrevs mot rummet innan spelet bröts ut i #67. De ligger kvar som skyddsnät för att beteendet inte ändrats,
// så här finns rummets gamla anrop, men de går genom rummets riktiga Act och WithGame.
public static class AuxCordRoom
{
    // Slumpen för uppspelningens ordning hör till spelet, men testerna seedar den när rummet skapas.
    private static readonly ConditionalWeakTable<GameRoom, Random> Randoms = [];

    public static GameRoom Create(RoomCode code, Random random)
    {
        var room = GameRoom.Create(code);
        Randoms.Add(room, random);
        return room;
    }

    extension(GameRoom room)
    {
        // Fasen som klienterna ser, från samma mappning som bilden.
        public GamePhase Phase => GameState.From(room, DateTimeOffset.MinValue).Phase;

        public AuxCordGame? Aux => room.Game as AuxCordGame;

        public AuxCordSettings AuxSettings => (AuxCordSettings)room.Settings;

        public int Round => room.Aux?.Round ?? 0;

        public string? Prompt => room.Stage == RoomStage.InGame ? room.Aux!.Prompt : null;

        public IReadOnlyCollection<string> UsedPrompts => room.UsedContent<AuxCordGame>();

        public IReadOnlyList<Entry> Entries => room.Aux?.Entries ?? [];

        public Entry? CurrentClip => room.Stage == RoomStage.InGame ? room.Aux!.CurrentClip : null;

        public int ClipNumber => room.Aux?.ClipNumber ?? 1;

        public bool SkippedRound => room.Stage == RoomStage.InGame && room.Aux!.SkippedRound;

        public Result UpdateSettings(string? hostToken, int? rounds, int? clipSeconds, int? submittingSeconds) =>
            room.UpdateSettings<AuxCordSettings>(hostToken, settings => settings.With(rounds, clipSeconds, submittingSeconds));

        public Result StartGame(string? hostToken, IReadOnlyList<string> prompts, DateTimeOffset now) =>
            room.StartGame(
                hostToken,
                settings => Result<IGame>.Success(new AuxCordGame(prompts, (AuxCordSettings)settings, Randoms.TryGetValue(room, out var random) ? random : Random.Shared)),
                now);

        public Result CanChooseTrack() => room.WithGame<AuxCordGame>(game => game.CanSearch());

        public Result SubmitTrack(string playerId, SubmittedTrack track, DateTimeOffset now) =>
            room.Act<AuxCordGame>(playerId, now, game => game.SubmitTrack(playerId, track, now));

        public Result CanVote() => room.WithGame<AuxCordGame>(game => game.CanVote());

        public Result Vote(string playerId, int entry, DateTimeOffset now) => room.Act<AuxCordGame>(playerId, now, game => game.Vote(playerId, entry));

        public Result<Entry> CurrentClipFor(string? hostToken) =>
            room.WithGameForHost<AuxCordGame, Entry>(hostToken, game => game.CurrentClip is { } entry ? entry : RoomErrors.WrongPhase);

        public Result RenewLink(long trackId, string previewUrl, DateTimeOffset now) =>
            room.WithGame<AuxCordGame>(game => game.RenewLink(trackId, previewUrl, now));

        public bool HasSubmitted(string playerId) => room.Aux?.HasSubmitted(playerId) ?? false;

        public bool HasVoted(string playerId) => room.Aux?.HasVoted(playerId) ?? false;

        public int? EntryNumberOf(string playerId) => room.Aux?.EntryNumberOf(playerId);

        public int? VoteOf(string playerId) => room.Aux?.VoteOf(playerId);

        public int VotesFor(int entry) => room.Aux?.VotesFor(entry) ?? 0;

        public int RoundPointsOf(string playerId) => room.Aux?.RoundPointsOf(playerId) ?? 0;
    }
}
