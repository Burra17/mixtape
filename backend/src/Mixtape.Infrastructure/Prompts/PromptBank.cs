using System.Text.Json;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Games.AuxCord;

namespace Mixtape.Infrastructure.Prompts;

// Promptarna i prompts.json. Filen är inbäddad i assemblyn, så att den inte kan saknas eller ligga på fel plats när appen körs.
public sealed class PromptBank : IPromptProvider
{
    private const string ResourceName = "prompts.json";

    private readonly IReadOnlyList<string> _prompts;
    private readonly Random _random;

    // Kastar om banken är trasig.
    public PromptBank(IReadOnlyList<string> prompts, Random random)
    {
        Validate(prompts);
        _prompts = prompts;
        _random = random;
    }

    // Ett spel kan ha flest rundor av alla val, och varje runda behöver en egen prompt.
    public static int MinPrompts => AuxCordSettings.RoundOptions.Max();

    public static PromptBank LoadEmbedded(Random random)
    {
        using var stream = typeof(PromptBank).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"{ResourceName} saknas i Mixtape.Infrastructure.");
        var prompts = JsonSerializer.Deserialize<List<string>>(stream) ?? throw new InvalidOperationException($"{ResourceName} är tom.");
        return new PromptBank(prompts, random);
    }

    public IReadOnlyList<string> Draw(int count, IReadOnlyCollection<string> used)
    {
        var unused = _prompts.Where(prompt => !used.Contains(prompt)).ToArray();
        var alreadyUsed = _prompts.Where(used.Contains).ToArray();
        _random.Shuffle(unused);
        _random.Shuffle(alreadyUsed);
        return unused.Concat(alreadyUsed).Take(count).ToList();
    }

    private static void Validate(IReadOnlyList<string> prompts)
    {
        if (prompts.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Promptbanken har en tom prompt.");
        }

        if (prompts.Select(prompt => prompt.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != prompts.Count)
        {
            throw new InvalidOperationException("Promptbanken har en dubblett.");
        }

        if (prompts.Count < MinPrompts)
        {
            throw new InvalidOperationException($"Promptbanken behöver minst {MinPrompts} promptar men har {prompts.Count}.");
        }
    }
}
