namespace NScreenplay.Core;

/// <summary>
/// Describes how an automation adapter should locate an interaction point in the UI or API.
/// </summary>
/// <remarks>
/// A single target may carry multiple strategies. Adapters select the most appropriate one.
/// This record is immutable and carries no Playwright (or other adapter) types.
/// </remarks>
public sealed record LocatorStrategy
{
    /// <summary>The strategy kind (CSS, XPath, Role, etc.).</summary>
    public LocatorStrategyKind Kind { get; }

    /// <summary>The primary selector or value for this strategy.</summary>
    public string Value { get; }

    /// <summary>Optional qualifier (e.g., the accessible name for a Role strategy).</summary>
    public string? Qualifier { get; }

    internal LocatorStrategy(LocatorStrategyKind kind, string value, string? qualifier = null)
    {
        Kind = kind;
        Value = value;
        Qualifier = qualifier;
    }
}

/// <summary>Identifies the mechanism used to locate an element or endpoint.</summary>
public enum LocatorStrategyKind
{
    /// <summary>CSS selector string.</summary>
    Css,

    /// <summary>XPath expression.</summary>
    XPath,

    /// <summary>ARIA role, optionally combined with accessible name via <see cref="LocatorStrategy.Qualifier"/>.</summary>
    Role,

    /// <summary>Form label text.</summary>
    Label,

    /// <summary>HTML id attribute.</summary>
    Id,

    /// <summary>Test-specific data attribute (e.g., data-testid).</summary>
    TestId,

    /// <summary>Visible text content.</summary>
    Text,

    /// <summary>Placeholder attribute value.</summary>
    Placeholder,

    /// <summary>Alt text for images.</summary>
    AltText,
}
