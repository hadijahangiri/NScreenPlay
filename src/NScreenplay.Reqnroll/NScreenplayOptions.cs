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

    /// <summary>When true, capture screenshot (and trace when available) on scenario failure.</summary>
    public bool CaptureOnFailure { get; init; } = false;

    /// <summary>Directory to write artifacts (relative to workspace root or absolute). If empty, uses a per-process temp directory.</summary>
    public string? ArtifactsDirectory { get; init; } = null;
}
