using NScreenplay.Playwright;
using Reqnroll;

namespace Login.Support;

/// <summary>
/// Provides the URL of the local test application HTML page to step definitions.
/// Uses Playwright's <c>data:</c> URI approach to serve the login page inline —
/// no web server or port required.
/// </summary>
public sealed class TestApp
{
    private static readonly string _loginHtml = LoadLoginHtml();

    /// <summary>
    /// Navigates the actor's browser to the self-contained login HTML page.
    /// </summary>
    public static async Task NavigateToLoginPage(NScreenplay.Core.Actor actor)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        await page.SetContentAsync(_loginHtml);
    }

    private static string LoadLoginHtml()
    {
        // Walk up from the assembly directory to find the TestApplication folder
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "TestApplication", "login.html");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException(
            "Could not locate TestApplication/login.html. " +
            "Ensure the file is set to 'Copy to Output Directory' in Login.csproj.");
    }
}
