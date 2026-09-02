using NScreenplay.Core;
using NScreenplay.Playwright;
using ReqnrollSmoke.Pages;

namespace ReqnrollSmoke.Consequences;

public sealed class SuccessMessageIsVisible : IConsequence
{
    private static readonly SuccessMessageIsVisible Instance = new();

    private SuccessMessageIsVisible()
    {
    }

    public static SuccessMessageIsVisible Now() => Instance;

    public async Task EvaluateAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var visible = await actor.AsksFor(Visibility.Of(SmokePage.Result), cancellationToken);
        if (!visible)
            throw new InvalidOperationException("Expected success message to be visible after submitting the smoke form.");
    }
}
