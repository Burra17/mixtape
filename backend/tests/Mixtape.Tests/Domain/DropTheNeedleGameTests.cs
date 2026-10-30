using Mixtape.Domain.Common;
using Mixtape.Domain.Games;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class DropTheNeedleGameTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Clip = TimeSpan.FromSeconds(DropTheNeedleSettings.Default.ClipSeconds);
    private static readonly TimeSpan Reveal = DropTheNeedleGame.RevealingDuration;
    private static readonly IReadOnlyList<SubmittedTrack> ThreeSongs = [TestTrack.Of(11, "Yellow"), TestTrack.Of(12, "In the End"), TestTrack.Of(13, "Hey Ya!")];

    private GameRoom _room = null!;
    private Player _anna = null!;
    private Player _bertil = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
        _room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle);
        _anna = _room.Join("Anna").Value;
        _bertil = _room.Join("Bertil").Value;
        _room.StartGame(_room.HostToken, settings => Result<IGame>.Success(new DropTheNeedleGame(ThreeSongs, (DropTheNeedleSettings)settings)), Now);
    }

    private DropTheNeedleGame Game => (DropTheNeedleGame)_room.Game!;

    [Test]
    public void Start_BeginsGuessingTheFirstSongForTheClipLength()
    {
        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Guessing));
        Assert.That(Game.SongNumber, Is.EqualTo(1));
        Assert.That(Game.SongCount, Is.EqualTo(3));
        Assert.That(_room.RemainingTime(Now), Is.EqualTo(Clip));
    }

    [Test]
    public void Start_PutsTheSongsInTheRoomsHistory()
    {
        Assert.That(_room.UsedContent<DropTheNeedleGame>(), Is.EqualTo(new[] { "11", "12", "13" }));
    }

    [Test]
    public void Guess_IsLockedAfterTheFirst()
    {
        Assert.That(GuessAs(_anna, 1, TestTrack.Of(99, "Fel")).IsSuccess, Is.True);

        Assert.That(GuessAs(_anna, 1, TestTrack.Of(11, "Yellow")).Error, Is.EqualTo(DropTheNeedleErrors.AlreadyGuessed));
        Assert.That(Game.GuessOf(_anna.Id)!.Track.Title, Is.EqualTo("Fel"));
    }

    [Test]
    public void Guess_KeepsWhenTheServerReceivedIt()
    {
        var receivedAt = Now.AddSeconds(3);

        _room.Act<DropTheNeedleGame>(_anna.Id, Now.AddSeconds(4), game => game.Guess(_anna.Id, 1, TestTrack.Of(11, "Yellow"), receivedAt));

        Assert.That(Game.GuessOf(_anna.Id)!.ReceivedAt, Is.EqualTo(receivedAt));
    }

    [Test]
    public void Guess_ForASongThatHasEnded_IsRejected()
    {
        _room.Tick(Now + Clip);

        Assert.That(GuessAs(_anna, 1, TestTrack.Of(11, "Yellow")).Error, Is.EqualTo(RoomErrors.WrongPhase));
        Assert.That(Game.HasGuessed(_anna.Id), Is.False);
    }

    [Test]
    public void Guess_ByEveryoneConnected_RevealsTheSongAtOnce()
    {
        GuessAs(_anna, 1, TestTrack.Of(11, "Yellow"));
        GuessAs(_bertil, 1, TestTrack.Of(12, "In the End"));

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));
        Assert.That(Game.SongNumber, Is.EqualTo(1));
        Assert.That(_room.RemainingTime(Now), Is.EqualTo(Reveal));
    }

    [Test]
    public void Guess_WhenTheOtherIsAway_RevealsTheSong()
    {
        _room.Disconnect(_bertil.Id, Now);

        GuessAs(_anna, 1, TestTrack.Of(11, "Yellow"));

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));
    }

    [Test]
    public void Guess_DuringPause_DoesNotMoveOnEarly()
    {
        _room.Pause(_room.HostToken, Now);

        GuessAs(_anna, 1, TestTrack.Of(11, "Yellow"));
        GuessAs(_bertil, 1, TestTrack.Of(11, "Yellow"));

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Guessing));
    }

    [Test]
    public void Tick_AtTheEndOfTheClip_RevealsTheSong()
    {
        Assert.That(_room.Tick(Now + Clip - TimeSpan.FromMilliseconds(1)), Is.False);
        Assert.That(_room.Tick(Now + Clip), Is.True);

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));
        Assert.That(Game.SongNumber, Is.EqualTo(1));
        Assert.That(_room.RemainingTime(Now + Clip), Is.EqualTo(Reveal));
    }

    [Test]
    public void Tick_AtTheEndOfTheReveal_StartsTheNextSong()
    {
        _room.Tick(Now + Clip);

        Assert.That(_room.Tick(Now + Clip + Reveal - TimeSpan.FromMilliseconds(1)), Is.False);
        Assert.That(_room.Tick(Now + Clip + Reveal), Is.True);

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Guessing));
        Assert.That(Game.SongNumber, Is.EqualTo(2));
        Assert.That(_room.RemainingTime(Now + Clip + Reveal), Is.EqualTo(Clip));
    }

    [Test]
    public void Tick_DuringAPausedReveal_DoesNotMoveOn()
    {
        _room.Tick(Now + Clip);
        _room.Pause(_room.HostToken, Now + Clip);

        _room.Tick(Now + Clip + Reveal * 2);

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));
    }

    [Test]
    public void Tick_AfterTheLastSong_RevealsItBeforeTheGameEnds()
    {
        var lastSong = SongStart(3);
        _room.Tick(lastSong + Clip);

        Assert.That(_room.Stage, Is.EqualTo(RoomStage.InGame));
        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));

        _room.Tick(lastSong + Clip + Reveal);

        Assert.That(_room.Stage, Is.EqualTo(RoomStage.GameOver));
    }

    [Test]
    public void GetCurrentClip_HasAnOldLink_SoItIsRenewedFirst()
    {
        var clip = Game.GetCurrentClip().Value;

        Assert.That(clip.Number, Is.EqualTo(1));
        Assert.That(clip.Track.Id, Is.EqualTo(11));
        Assert.That(clip.NeedsNewLink(Now), Is.True);
    }

    [Test]
    public void RefreshTrack_GivesTheClipTheNewLinkAndContributors()
    {
        Game.RefreshTrack(11, "https://ny", ["Coldplay"], Now);

        var clip = Game.GetCurrentClip().Value;
        Assert.That(clip.Track.PreviewUrl, Is.EqualTo("https://ny"));
        Assert.That(clip.NeedsNewLink(Now), Is.False);
        Assert.That(clip.Track.Contributors, Is.EqualTo(new[] { "Coldplay" }));
    }

    [Test]
    public void SkipUnplayableClip_RemovesTheSongAndStartsTheNext()
    {
        var secondSong = SongStart(2);
        var revision = _room.Revision;

        _room.SkipUnplayableClip(_room.HostToken, 12, secondSong + TimeSpan.FromSeconds(2));

        Assert.That(Game.SongNumber, Is.EqualTo(2));
        Assert.That(Game.SongCount, Is.EqualTo(2));
        Assert.That(Game.ClipNumber, Is.EqualTo(3));
        Assert.That(Game.GetCurrentClip().Value.Track.Id, Is.EqualTo(13));
        Assert.That(Game.GetCurrentClip().Value.Number, Is.EqualTo(3));
        Assert.That(_room.RemainingTime(secondSong + TimeSpan.FromSeconds(2)), Is.EqualTo(Clip));
        Assert.That(_room.Revision, Is.GreaterThan(revision));
    }

    [Test]
    public void Guess_ForTheClipBeforeASkip_IsRejected()
    {
        var clip = Game.CanGuess(_anna.Id).Value;
        _room.SkipUnplayableClip(_room.HostToken, 11, Now);

        Assert.That(GuessAs(_anna, clip, TestTrack.Of(11, "Yellow")).Error, Is.EqualTo(RoomErrors.WrongPhase));
        Assert.That(Game.HasGuessed(_anna.Id), Is.False);
    }

    [Test]
    public void SkipUnplayableClip_TheLastSong_EndsTheGame()
    {
        var lastSong = SongStart(3);

        _room.SkipUnplayableClip(_room.HostToken, 13, lastSong);

        Assert.That(_room.Stage, Is.EqualTo(RoomStage.GameOver));
    }

    [Test]
    public void SkipUnplayableClip_ForASongThatIsNotPlaying_DoesNothing()
    {
        _room.SkipUnplayableClip(_room.HostToken, 13, Now);

        Assert.That(Game.SongCount, Is.EqualTo(3));
        Assert.That(Game.SongNumber, Is.EqualTo(1));
    }

    [Test]
    public void SkipUnplayableClip_DuringTheReveal_DoesNothing()
    {
        _room.Tick(Now + Clip);

        _room.SkipUnplayableClip(_room.HostToken, 11, Now + Clip);

        Assert.That(Game.Phase, Is.EqualTo(DropTheNeedlePhase.Revealing));
        Assert.That(Game.SongCount, Is.EqualTo(3));
    }

    [Test]
    public void SkipUnplayableClip_WithWrongHostToken_ReturnsRoomNotFound()
    {
        Assert.That(_room.SkipUnplayableClip("fel", 11, Now).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(Game.SongCount, Is.EqualTo(3));
    }

    [Test]
    public void CanSearch_OnlyWhileGuessing()
    {
        Assert.That(_room.WithGame<ISearchesTracks>(game => game.CanSearch()).IsSuccess, Is.True);

        _room.Tick(Now + Clip);

        Assert.That(_room.WithGame<ISearchesTracks>(game => game.CanSearch()).Error, Is.EqualTo(RoomErrors.WrongPhase));

        _room.End(_room.HostToken, Now);

        Assert.That(_room.WithGame<ISearchesTracks>(game => game.CanSearch()).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    // Låter varje gissning och avslöjande före låt nummer song gå ut, och ger tiden när den låten börjar.
    private DateTimeOffset SongStart(int song)
    {
        var at = Now;
        for (var i = 1; i < song; i++)
        {
            _room.Tick(at += Clip);
            _room.Tick(at += Reveal);
        }

        return at;
    }

    private Result GuessAs(Player player, int songNumber, SubmittedTrack track) =>
        _room.Act<DropTheNeedleGame>(player.Id, Now, game => game.Guess(player.Id, songNumber, track, Now));
}
