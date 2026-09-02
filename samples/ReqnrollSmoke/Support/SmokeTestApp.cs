using NScreenplay.Core;
using NScreenplay.Playwright;

namespace ReqnrollSmoke.Support;

public static class SmokeTestApp
{
    private static readonly string Html = LoadSmokeHtml();

    public static async Task OpenAsync(Actor actor)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        await page.SetContentAsync(Html);
    }

    private static string LoadSmokeHtml()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "TestApplication", "smoke.html");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
            dir = Path.GetDirectoryName(dir);
        }

        throw new FileNotFoundException("Could not locate TestApplication/smoke.html. Ensure it is copied to output.");
    }
}
