namespace Mixtape.Application.Rooms;

public interface IPromptProvider
{
    // count olika promptar. De som inte finns i used dras först, och använda tas bara när banken inte räcker.
    IReadOnlyList<string> Draw(int count, IReadOnlyCollection<string> used);
}
