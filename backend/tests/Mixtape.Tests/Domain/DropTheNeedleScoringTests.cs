using Mixtape.Domain.Common;
using Mixtape.Domain.Games;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class DropTheNeedleScoringTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Guessing = DropTheNeedleSettings.Default.GuessLength;
    private static readonly TimeSpan Reveal = DropTheNeedleGame.RevealingDuration;
    private static readonly IReadOnlyList<SubmittedTrack> TwoSongs = [TestTrack.Of(11, "Yellow", "Coldplay"), TestTrack.Of(12, "In the End", "Linkin Park")];

    private GameRoom _room = null!;
    private Player _anna = null!;
    private Player _bertil = null!;
    private Player _cecilia = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
        _room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle);
        _anna = _room.Join("Anna").Value;
        _bertil = _room.Join("Bertil").Value;
        _cecilia = _room.Join("Cecilia").Value;
        _room.StartGame(_room.HostToken, settings => Result<IGame>.Success(new DropTheNeedleGame(TwoSongs, (DropTheNeedleSettings)settings)), Now);
    }

    private DropTheNeedleGame Game => (DropTheNeedleGame)_room.Game!;

    [Test]
    public void TheEndOfTheSong_GivesTwoForTheSongOneForTheArtistAndOneExtraToTheFastest()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow (Live)", "Coldplay"), Now.AddSeconds(5));
        GuessAs(_bertil, TestTrack.Of(91, "Fix You", "Coldplay"), Now.AddSeconds(2));
        GuessAs(_cecilia, TestTrack.Of(92, "Numb", "Linkin Park"), Now.AddSeconds(1));

        Assert.That(_anna.Score, Is.EqualTo(3));
        Assert.That(_bertil.Score, Is.EqualTo(1));
        Assert.That(_cecilia.Score, Is.Zero);
    }

    [Test]
    public void TheEndOfTheSong_GivesTheBonusOnlyToTheFastestRightSong()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(5));
        GuessAs(_bertil, TestTrack.Of(91, "Yellow - Remastered", "Coldplay"), Now.AddSeconds(3));
        _room.Tick(Now + Guessing);

        Assert.That(_anna.Score, Is.EqualTo(2));
        Assert.That(_bertil.Score, Is.EqualTo(3));
    }

    [Test]
    public void TheEndOfTheSong_WithTheSameTime_GivesTheBonusToBoth()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(4));
        GuessAs(_bertil, TestTrack.Of(91, "Yellow", "Coldplay"), Now.AddSeconds(4));
        _room.Tick(Now + Guessing);

        Assert.That(_anna.Score, Is.EqualTo(3));
        Assert.That(_bertil.Score, Is.EqualTo(3));
    }

    [Test]
    public void TheEndOfTheSong_KeepsTheResultForEveryPlayer()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(1));
        GuessAs(_bertil, TestTrack.Of(91, "Fix You", "Coldplay"), Now.AddSeconds(2));
        _room.Tick(Now + Guessing);

        var results = Game.LastResults;
        Assert.That(results.Single(result => result.PlayerId == _anna.Id), Is.EqualTo(new SongResult(_anna.Id, GuessScore.Song, true, 3, TestTrack.Of(90, "Yellow", "Coldplay"))));
        Assert.That(results.Single(result => result.PlayerId == _bertil.Id), Is.EqualTo(new SongResult(_bertil.Id, GuessScore.Artist, false, 1, TestTrack.Of(91, "Fix You", "Coldplay"))));
        Assert.That(results.Single(result => result.PlayerId == _cecilia.Id), Is.EqualTo(new SongResult(_cecilia.Id, GuessScore.None, false, 0, null)));
        Assert.That(Game.LastSong!.Title, Is.EqualTo("Yellow"));
        Assert.That(Game.RoundPointsOf(_anna.Id), Is.EqualTo(3));
    }

    [Test]
    public void WithoutTheFastestBonus_TheRightSongGivesTwoAndNobodyIsFastest()
    {
        var room = GameRoom.Create(RoomCode.Parse("GHJK")!);
        room.ChooseMode(room.HostToken, GameMode.DropTheNeedle);
        var anna = room.Join("Anna").Value;
        room.Join("Bertil");
        room.UpdateSettings<DropTheNeedleSettings>(room.HostToken, settings => settings.With(null, null, null, false, null));
        room.StartGame(room.HostToken, settings => Result<IGame>.Success(new DropTheNeedleGame(TwoSongs, (DropTheNeedleSettings)settings)), Now);

        room.Act<DropTheNeedleGame>(anna.Id, Now.AddSeconds(1), game => game.Guess(anna.Id, game.ClipNumber, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(1)));
        room.Tick(Now + Guessing);

        Assert.That(anna.Score, Is.EqualTo(2));
        Assert.That(((DropTheNeedleGame)room.Game!).LastResults.Any(result => result.Fastest), Is.False);
    }

    [Test]
    public void ASkippedSong_GivesNoPoints()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(1));

        _room.SkipUnplayableClip(_room.HostToken, 11, Now.AddSeconds(2));

        Assert.That(_anna.Score, Is.Zero);
        Assert.That(Game.LastResults, Is.Empty);
    }

    [Test]
    public void ASongEndedEarlyByTheHost_GivesNoPoints()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(1));

        _room.End(_room.HostToken, Now.AddSeconds(2));

        Assert.That(_anna.Score, Is.Zero);
        Assert.That(_room.Stage, Is.EqualTo(RoomStage.GameOver));
    }

    [Test]
    public void AGameEndedDuringTheReveal_KeepsThePointsForTheSong()
    {
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(1));
        _room.Tick(Now + Guessing);

        _room.End(_room.HostToken, Now + Guessing + TimeSpan.FromSeconds(2));

        Assert.That(_anna.Score, Is.EqualTo(3));
        Assert.That(_room.Stage, Is.EqualTo(RoomStage.GameOver));
    }

    [Test]
    public void TheNextSong_AddsToTheScore()
    {
        var secondSong = Now + Guessing + Reveal;
        GuessAs(_anna, TestTrack.Of(90, "Yellow", "Coldplay"), Now.AddSeconds(1));
        _room.Tick(Now + Guessing);
        _room.Tick(secondSong);

        GuessAs(_anna, TestTrack.Of(93, "In The End", "Linkin Park"), secondSong + TimeSpan.FromSeconds(1));
        _room.Tick(secondSong + Guessing);

        Assert.That(_anna.Score, Is.EqualTo(6));
        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));
    }

    private void GuessAs(Player player, SubmittedTrack track, DateTimeOffset receivedAt) =>
        _room.Act<DropTheNeedleGame>(player.Id, receivedAt, game => game.Guess(player.Id, game.ClipNumber, track, receivedAt));
}
