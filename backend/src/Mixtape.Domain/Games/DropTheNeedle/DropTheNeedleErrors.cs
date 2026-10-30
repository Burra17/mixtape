using Mixtape.Domain.Common;

namespace Mixtape.Domain.Games.DropTheNeedle;

public static class DropTheNeedleErrors
{
    // En spellista kan krympa när låtar försvinner från Deezer.
    public static readonly Error TooFewSongs = new("drop_the_needle.too_few_songs", "Kategorin har för få låtar just nu.");

    public static readonly Error AlreadyGuessed = new("drop_the_needle.already_guessed", "Du har redan gissat.");
}
