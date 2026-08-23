namespace NScreenplay.Reqnroll;

/// <summary>
/// Configuration for NScreenplay browser automation in Reqnroll scenarios.
/// </summary>
/// <remarks>
/// Provide custom values via <see cref="NScreenplayConfiguration.Configure"/> or use the defaults.
/// </remarks>
public sealed record NScreenplayOptions
{
    /// <summary>Browser channel to launch. Defaults to Chromium.</summary>
    public string Browser { get; init; } = "chromium";

    /// <summary>Run without a visible browser window. Defaults to true for CI.</summary>
    public bool Headless { get; init; } = true;

    /// <summary>Base URL used by relative navigation targets.</summary>
    public string BaseUrl { get; init; } = "https://localhost";

    /// <summary>Default navigation timeout in milliseconds.</summary>
    public int TimeoutMilliseconds { get; init; } = 30_000;
}
