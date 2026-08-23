namespace NScreenplay.Reqnroll;

/// <summary>
/// Global configuration entry point for NScreenplay Reqnroll integration.
/// </summary>
public static class NScreenplayConfiguration
{
    /// <summary>Current options. Set once at test assembly startup if customization is needed.</summary>
    public static NScreenplayOptions Options { get; private set; } = new();

    /// <summary>
    /// Overrides the default options. Call from a test assembly initializer
    /// (e.g., <c>[BeforeTestRun]</c>) before any scenario runs.
    /// </summary>
    public static void Configure(NScreenplayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }
}
