using Mixtape.Infrastructure.Prompts;

namespace Mixtape.Tests.Infrastructure;

public sealed class PromptBankTests
{
    private static readonly IReadOnlyList<string> TenPrompts = Enumerable.Range(1, 10).Select(i => $"Prompt {i}").ToList();

    [Test]
    public void LoadEmbedded_TheRealBank_IsValidAndHasEnoughForTheLongestGame()
    {
        var bank = PromptBank.LoadEmbedded(new Random(1));

        Assert.That(bank.Draw(PromptBank.MinPrompts, []), Has.Count.EqualTo(PromptBank.MinPrompts));
    }

    [Test]
    public void Draw_AWholeGameOfEightRounds_GivesEightDifferentPrompts()
    {
        var drawn = PromptBank.LoadEmbedded(new Random(1)).Draw(8, []);

        Assert.That(drawn, Is.Unique);
        Assert.That(drawn, Has.Count.EqualTo(8));
    }

    [Test]
    public void Draw_TakesUnusedPromptsBeforeUsedOnes()
    {
        var used = TenPrompts.Take(7).ToHashSet();

        var drawn = new PromptBank(TenPrompts, new Random(1)).Draw(5, used);

        Assert.That(drawn.Take(3), Is.EquivalentTo(TenPrompts.Skip(7)));
        Assert.That(drawn.Skip(3), Is.SubsetOf(used));
        Assert.That(drawn, Is.Unique);
    }

    [Test]
    public void Draw_WithNothingUsed_ShufflesTheBank()
    {
        var bank = new PromptBank(TenPrompts, new Random(1));

        Assert.That(bank.Draw(10, []), Is.EquivalentTo(TenPrompts).And.Not.EqualTo(TenPrompts));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void New_WithAnEmptyPrompt_Throws(string empty)
    {
        Assert.Throws<InvalidOperationException>(() => _ = new PromptBank([.. TenPrompts, empty], new Random(1)));
    }

    [Test]
    public void New_WithADuplicate_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _ = new PromptBank([.. TenPrompts, " prompt 1 "], new Random(1)));
    }

    [Test]
    public void New_WithFewerPromptsThanTheLongestGame_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _ = new PromptBank(TenPrompts.Take(PromptBank.MinPrompts - 1).ToList(), new Random(1)));
    }
}
