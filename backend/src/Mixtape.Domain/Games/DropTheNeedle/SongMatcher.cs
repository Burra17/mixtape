using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Mixtape.Domain.Music;

namespace Mixtape.Domain.Games.DropTheNeedle;

// Bedömer en gissning mot rätt låt på titel och artist, eftersom samma låt finns på Deezer i flera versioner med olika id.
// Live, remaster och radio edit räknas som samma låt. En cover av en annan artist räknas som fel.
public static partial class SongMatcher
{
    public static GuessScore Score(SubmittedTrack guess, SubmittedTrack correct)
    {
        if (!Artists(guess).Overlaps(Artists(correct)))
        {
            return GuessScore.None;
        }

        return NormalizeTitle(guess.Title) == NormalizeTitle(correct.Title) ? GuessScore.Song : GuessScore.Artist;
    }

    // Titeln utan version och medverkande: det inom parentes eller hakparentes, allt efter " - " och allt från "feat.".
    // En titel som bara är en version, som "(Intro)", jämförs hel, så att den inte blir tom och matchar allt.
    private static string NormalizeTitle(string title)
    {
        var withoutVersion = Bracketed().Replace(title, " ");
        withoutVersion = withoutVersion.Split(" - ")[0];
        withoutVersion = Featuring().Split(withoutVersion)[0];
        var normalized = Key(withoutVersion);
        return normalized.Length > 0 ? normalized : Key(title);
    }

    // "Beyoncé" = "Beyonce", "AC/DC" = "ACDC", "The Beatles" = "Beatles" och "&" = "and".
    private static string NormalizeArtist(string artist)
    {
        var words = Simplify(artist.Replace("&", " and "));
        return Key(words.StartsWith("the ", StringComparison.Ordinal) ? words[4..] : words);
    }

    // Utan mellanslag, så att "Jay-Z" = "Jay Z" och "Hard-Knock Life" = "Hard Knock Life".
    private static string Key(string text) => Simplify(text).Replace(" ", string.Empty);

    // Alla medverkande artister, eller huvudartisten när Deezer inte gett några.
    private static HashSet<string> Artists(SubmittedTrack track) =>
        (track.Contributors is { Count: > 0 } contributors ? contributors : [track.Artist]).Select(NormalizeArtist).ToHashSet();

    // Gemener, utan accenter och skiljetecken, och med ett blanksteg mellan orden.
    private static string Simplify(string text)
    {
        var letters = new StringBuilder();
        foreach (var character in text.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                letters.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                letters.Append(' ');
            }
        }

        return string.Join(' ', letters.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [GeneratedRegex(@"\([^)]*\)|\[[^\]]*\]")]
    private static partial Regex Bracketed();

    [GeneratedRegex(@"\s(?:feat\.?|ft\.|featuring)\s", RegexOptions.IgnoreCase)]
    private static partial Regex Featuring();
}
