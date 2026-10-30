using Mixtape.Domain.Common;
using Mixtape.Domain.Music;
using Mixtape.Domain.Rooms;

namespace Mixtape.Domain.Games.DropTheNeedle;

// Drop the Needle: storskärmen spelar ett klipp och alla gissar låten genom att söka fram den i mobilen.
// Poängen kommer i #71 och avslöjandet efter varje låt i #72.
public sealed class DropTheNeedleGame : IPlaysClips, ISearchesTracks
{
    private readonly DropTheNeedleSettings _settings;

    // Låtarna i spelordning. Number är klippets plats i listan och ändras aldrig, även när en låt hoppas över.
    private readonly List<CurrentClip> _songs;

    // Platserna för låtar som inte gick att spela. De räknas inte i låtens nummer eller i antalet låtar.
    private readonly HashSet<int> _skipped = [];

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

    // Klippet som spelas, 1 för det första. Byts vid varje nytt klipp, också när en låt hoppas över,
    // så att storskärmen hämtar det nya klippet och en gissning aldrig hamnar på en annan låt än den som spelades.
    public int ClipNumber => _index + 1;

    // Låtens nummer som spelarna ser, 1 för den första. En låt som hoppats över räknas inte.
    public int SongNumber => ClipNumber - _skipped.Count(skipped => skipped < _index);

    public int SongCount => _songs.Count - _skipped.Count;

    public bool HasGuessed(string playerId) => _guesses.ContainsKey(playerId);

    // Spelarens gissning på låten som spelas, eller null.
    public Guess? GuessOf(string playerId) => _guesses.GetValueOrDefault(playerId);

    public Step Start(IReadOnlyList<Player> players) => Step.Next(ClipLength);

    public Result CanSearch() => Phase == DropTheNeedlePhase.Guessing ? Result.Success() : RoomErrors.WrongPhase;

    // Spelaren kan bara gissa en gång per låt, och det kontrolleras innan låten hämtas från Deezer. Ger klippets nummer, som gissningen sedan gäller.
    public Result<int> CanGuess(string playerId)
    {
        if (Phase != DropTheNeedlePhase.Guessing)
        {
            return RoomErrors.WrongPhase;
        }

        return HasGuessed(playerId) ? DropTheNeedleErrors.AlreadyGuessed : ClipNumber;
    }

    // clipNumber är klippet som spelades när gissningen kom fram. Har det hunnit bytas medan låten hämtades från Deezer nekas gissningen.
    public Result Guess(string playerId, int clipNumber, SubmittedTrack track, DateTimeOffset receivedAt)
    {
        if (Phase != DropTheNeedlePhase.Guessing || clipNumber != ClipNumber)
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

    // I ett quiz är ett tyst klipp värre än i Aux Cord Battles, så låten räknas bort och nästa börjar direkt.
    public Step? SkipUnplayable(long trackId)
    {
        if (Phase != DropTheNeedlePhase.Guessing || _songs[_index].Track.Id != trackId)
        {
            return null;
        }

        _skipped.Add(_index);
        return CompletePhase();
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
}
