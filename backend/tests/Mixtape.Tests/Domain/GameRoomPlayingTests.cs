using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomPlayingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private const int Seed = 17;

    private GameRoom _room = null!;
    private DateTimeOffset _now;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!, new Random(Seed));
        _now = Now;
        foreach (var name in new[] { "Anna", "Bertil", "Cecilia" })
        {
            _room.Join(name);
        }

        _room.StartGame(_room.HostToken, Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList(), _now);
    }

    [Test]
    public void Playing_PlaysAllEntriesInTheOrderOfTheSeededRandom()
    {
        var submitted = SubmitForAll();

        var expected = submitted.ToArray();
        new Random(Seed).Shuffle(expected);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.Entries.Select(entry => entry.Track), Is.EqualTo(expected));
        Assert.That(_room.CurrentClip!.Track, Is.EqualTo(expected[0]));
        Assert.That(_room.ClipNumber, Is.EqualTo(1));
    }

    [Test]
    public void Tick_GoesToTheNextClipAfterTheClipLengthAndThenToVoting()
    {
        SubmitForAll();
        var clip = _room.Settings.ClipLength;

        for (var number = 2; number <= 3; number++)
        {
            Assert.That(_room.Tick(_now + clip - TimeSpan.FromMilliseconds(1)), Is.False);
            _now += clip;
            Assert.That(_room.Tick(_now), Is.True);
            Assert.That(_room.ClipNumber, Is.EqualTo(number));
            Assert.That(_room.CurrentClip, Is.SameAs(_room.Entries[number - 1]));
            Assert.That(_room.RemainingTime(_now), Is.EqualTo(clip));
        }

        _now += clip;
        Assert.That(_room.Tick(_now), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));
        Assert.That(_room.CurrentClip, Is.Null);
    }

    [Test]
    public void Tick_FollowsTheClipLengthFromTheSettings()
    {
        var room = GameRoom.Create(RoomCode.Parse("BCDF")!, Random.Shared);
        foreach (var name in new[] { "Anna", "Bertil", "Cecilia" })
        {
            room.Join(name);
        }

        room.UpdateSettings(room.HostToken, 3, 20, null);
        room.StartGame(room.HostToken, ["A", "B", "C"], _now);
        foreach (var player in room.Players)
        {
            room.SubmitTrack(player.Id, TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}"), _now);
        }

        Assert.That(room.RemainingTime(_now), Is.EqualTo(TimeSpan.FromSeconds(20)));
    }

    [Test]
    public void DisconnectHost_FreezesTheClip()
    {
        SubmitForAll();
        _room.DisconnectHost(_now.AddSeconds(4));

        Assert.That(_room.Tick(_now.AddMinutes(20)), Is.False);
        Assert.That(_room.ClipNumber, Is.EqualTo(1));

        var back = _now.AddMinutes(20);
        _room.ResumeHost(_room.HostToken, back);

        Assert.That(_room.RemainingTime(back), Is.EqualTo(_room.Settings.ClipLength - TimeSpan.FromSeconds(4)));
    }

    [Test]
    public void CurrentClip_IsNullOutsidePlaying()
    {
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.CurrentClip, Is.Null);
    }

    [Test]
    public void SubmitTrack_StoresTheLinkAndWhenItWasFetched()
    {
        var anna = _room.Players[0];

        _room.SubmitTrack(anna.Id, TestTrack.Of(1, "Ett"), _now);

        var entry = _room.Entries.Single();
        Assert.That(entry.Track.PreviewUrl, Is.EqualTo("https://klipp/1"));
        Assert.That(entry.LinkFetchedAt, Is.EqualTo(_now));
        Assert.That(entry.NeedsNewLink(_now + Entry.LinkMaxAge - TimeSpan.FromSeconds(1)), Is.False);
        Assert.That(entry.NeedsNewLink(_now + Entry.LinkMaxAge), Is.True);
    }

    [Test]
    public void CurrentClipFor_TheHost_ReturnsTheClip()
    {
        SubmitForAll();

        Assert.That(_room.CurrentClipFor(_room.HostToken).Value, Is.SameAs(_room.CurrentClip));
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void CurrentClipFor_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        SubmitForAll();

        Assert.That(_room.CurrentClipFor(token).Error, Is.EqualTo(RoomErrors.NotFound));
    }

    [Test]
    public void CurrentClipFor_OutsidePlaying_ReturnsWrongPhase()
    {
        Assert.That(_room.CurrentClipFor(_room.HostToken).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    [Test]
    public void RenewLink_ReplacesTheLinkAndItsTime()
    {
        SubmitForAll();
        var later = _now.AddMinutes(11);

        _room.RenewLink(2, "https://ny", later);

        var entry = _room.Entries.Single(entry => entry.Track.Id == 2);
        Assert.That(entry.Track.PreviewUrl, Is.EqualTo("https://ny"));
        Assert.That(entry.LinkFetchedAt, Is.EqualTo(later));
    }

    [Test]
    public void RenewLink_OutsidePlaying_ReturnsWrongPhase()
    {
        Assert.That(_room.RenewLink(1, "https://ny", _now).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    // Alla tre skickar in, så att rummet går direkt till uppspelningen. Ger låtarna i den ordning de kom in.
    private List<SubmittedTrack> SubmitForAll()
    {
        var tracks = _room.Players.Select(player => TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}")).ToList();
        for (var i = 0; i < tracks.Count; i++)
        {
            _room.SubmitTrack(_room.Players[i].Id, tracks[i], _now);
        }

        return tracks;
    }
}
