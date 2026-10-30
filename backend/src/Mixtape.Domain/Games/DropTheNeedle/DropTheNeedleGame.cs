using Mixtape.Domain.Common;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games.DropTheNeedle;

// Drop the Needle: storskärmen spelar ett klipp och alla gissar låten genom att söka fram den i mobilen.
// Poängen kommer i #71 och avslöjandet efter varje låt i #72.
public sealed class DropTheNeedleGame : IPlaysClips, ISearchesTracks
{
    private readonly DropTheNeedleSettings _settings;

    // Låtarna i spelordning. Den som inte går att spela tas bort, så att antalet låtar stämmer med det som spelades.
    private readonly List<CurrentClip> _songs;

    // Den här låtens gissningar, per spelare.
    private readonly Dictionary<string, Guess> _guesses = [];

    private int _index;

    // songs är lottade av servern. Länkarna kan komma från en cachad spellista, så de räknas som gamla och förnyas innan klippet spelas.
    public DropTheNeedleGame(IReadOnlyList<SubmittedTrack> songs, DropTheNeedleSettings settings)
    {
        if (songs.Count == 0)
        {
            throw new ArgumentException("Ett spel behöver minst en låt.", nameof(songs));
        }

        _songs = songs.Select((song, index) => new CurrentClip(index + 1, song, DateTimeOffset.MinValue)).ToList();
        _settings = settings;
        UsedContent = songs.Select(SongDraw.Key).ToList();
    }

    public DropTheNeedlePhase Phase { get; private set; }

    public IReadOnlyCollection<string> UsedContent { get; }

    public string CategoryId => _settings.CategoryId;

    // 1 för den första låten.
    public int SongNumber => _index + 1;

    public int SongCount => _songs.Count;

    public bool HasGuessed(string playerId) => _guesses.ContainsKey(playerId);

    // Spelarens gissning på låten som spelas, eller null.
    public Guess? GuessOf(string playerId) => _guesses.GetValueOrDefault(playerId);

    public Step Start(IReadOnlyList<Player> players) => Step.Next(ClipLength);

    public Result CanSearch() => Phase == DropTheNeedlePhase.Guessing ? Result.Success() : RoomErrors.WrongPhase;

    // Spelaren kan bara gissa en gång per låt, och det kontrolleras innan låten hämtas från Deezer. Ger låtens nummer, som gissningen sedan gäller.
    public Result<int> CanGuess(string playerId)
    {
        if (Phase != DropTheNeedlePhase.Guessing)
        {
            return RoomErrors.WrongPhase;
        }

        return HasGuessed(playerId) ? DropTheNeedleErrors.AlreadyGuessed : SongNumber;
    }

    // songNumber är låten som spelades när gissningen kom fram. Har den hunnit bytas medan låten hämtades från Deezer nekas gissningen.
    public Result Guess(string playerId, int songNumber, SubmittedTrack track, DateTimeOffset receivedAt)
    {
        if (Phase != DropTheNeedlePhase.Guessing || songNumber != SongNumber)
        {
            return RoomErrors.WrongPhase;
        }

        if (HasGuessed(playerId))
        {
            return DropTheNeedleErrors.AlreadyGuessed;
        }

        _guesses[playerId] = new Guess(playerId, track, receivedAt);
        return Result.Success();
    }

    public Result<CurrentClip> GetCurrentClip() => Phase == DropTheNeedlePhase.Guessing ? _songs[_index] : RoomErrors.WrongPhase;

    public Result RenewLink(long trackId, string previewUrl, DateTimeOffset now)
    {
        if (Phase != DropTheNeedlePhase.Guessing)
        {
            return RoomErrors.WrongPhase;
        }

        var index = _songs.FindIndex(song => song.Track.Id == trackId);
        if (index >= 0)
        {
            _songs[index] = _songs[index] with { Track = _songs[index].Track with { PreviewUrl = previewUrl }, LinkFetchedAt = now };
        }

        return Result.Success();
    }

    // I ett quiz är ett tyst klipp värre än i Aux Cord Battles, så låten tas bort och nästa börjar direkt.
    public Step? SkipUnplayable(long trackId)
    {
        if (Phase != DropTheNeedlePhase.Guessing || _songs[_index].Track.Id != trackId)
        {
            return null;
        }

        _songs.RemoveAt(_index);
        _guesses.Clear();
        Renumber();
        return _index < _songs.Count ? Step.Next(ClipLength) : Step.Finished;
    }

    public bool IsEveryoneDone(IReadOnlyList<Player> connected) =>
        Phase == DropTheNeedlePhase.Guessing && connected.Count > 0 && connected.All(player => HasGuessed(player.Id));

    public Step CompletePhase()
    {
        if (_index >= _songs.Count - 1)
        {
            return Step.Finished;
        }

        _index++;
        _guesses.Clear();
        return Step.Next(ClipLength);
    }

    // Poängen för en avbruten låt räknas inte. Än finns inga poäng, så gissningarna tas bara bort.
    public void AbortRound() => _guesses.Clear();

    private TimeSpan ClipLength => TimeSpan.FromSeconds(_settings.ClipSeconds);

    private void Renumber()
    {
        for (var i = 0; i < _songs.Count; i++)
        {
            _songs[i] = _songs[i] with { Number = i + 1 };
        }
    }
}
