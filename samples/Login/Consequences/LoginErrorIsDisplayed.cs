using Login.Pages;
using NScreenplay.Core;
using NScreenplay.Playwright;

namespace Login.Consequences;

/// <summary>
/// Verifies that the login error message is visible, confirming a failed login attempt.
/// </summary>
public sealed class LoginErrorIsDisplayed : IConsequence
{
    private static readonly LoginErrorIsDisplayed _instance = new();

    private LoginErrorIsDisplayed() { }

    /// <summary>Returns the singleton consequence instance.</summary>
    public static LoginErrorIsDisplayed Now() => _instance;

    /// <inheritdoc/>
    public async Task EvaluateAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var isVisible = await actor.AsksFor(Visibility.Of(LoginPage.ErrorMessage), cancellationToken);
        if (!isVisible)
            throw new InvalidOperationException(
                "Expected the login error message to be visible, but it was not.");
    }

    /// <inheritdoc/>
    public override string ToString() => "Login error is displayed";
}
