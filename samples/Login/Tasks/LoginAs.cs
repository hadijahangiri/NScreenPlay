using Login.Tasks;
using NScreenplay.Core;
using NScreenplay.Playwright;

namespace Login.Tasks;

/// <summary>
/// Convenience factory for well-known credential sets used in scenarios.
/// Keeps Gherkin-level step definitions free from credential literals.
/// </summary>
public static class LoginAs
{
    // Test credentials (demo only — not real secrets)
    private const string ValidUsername = "alice@example.com";
    private const string ValidPassword = "secret123";

    /// <summary>Returns a task that logs in with the valid demo credentials.</summary>
    public static LoginWithCredentials ValidUser() =>
        LoginWithCredentials.Using(ValidUsername, ValidPassword);

    /// <summary>Returns a task that logs in with deliberately wrong credentials.</summary>
    public static LoginWithCredentials InvalidUser() =>
        LoginWithCredentials.Using("wrong@example.com", "notthepassword");
}
