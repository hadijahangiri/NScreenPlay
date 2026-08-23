namespace NScreenplay.Core;

/// <summary>
/// Base exception for errors originating from the NScreenplay framework.
/// </summary>
public class ScreenplayException : Exception
{
    /// <inheritdoc/>
    public ScreenplayException(string message) : base(message) { }

    /// <inheritdoc/>
    public ScreenplayException(string message, Exception innerException) : base(message, innerException) { }
}
