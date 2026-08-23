using Login.Pages;
using NScreenplay.Core;
using NScreenplay.Playwright;

namespace Login.Consequences;

/// <summary>
/// Verifies that the dashboard heading is visible, confirming a successful login.
/// </summary>
public sealed class DashboardIsDisplayed : IConsequence
{
    private static readonly DashboardIsDisplayed _instance = new();

    private DashboardIsDisplayed() { }

    /// <summary>Returns the singleton consequence instance.</summary>
    public static DashboardIsDisplayed Now() => _instance;

    /// <inheritdoc/>
    public async Task EvaluateAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var isVisible = await actor.AsksFor(Visibility.Of(DashboardPage.Heading), cancellationToken);
        if (!isVisible)
            throw new InvalidOperationException(
                "Expected the dashboard heading to be visible, but it was not.");
    }

    /// <inheritdoc/>
    public override string ToString() => "Dashboard is displayed";
}
