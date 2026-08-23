namespace NScreenplay.Core;

/// <summary>
/// A semantic description of something an automation adapter can interact with.
/// </summary>
/// <remarks>
/// <para>
/// A Target carries a human-readable name and one or more <see cref="LocatorStrategy"/> values.
/// Adapters (e.g., NScreenplay.Playwright) translate these strategies into their
/// native locator types — no Playwright types ever appear here.
/// </para>
/// <para>
/// Targets are immutable value objects. The fluent builder returns new instances at each step.
/// Store targets as static members on page object classes for reuse.
/// </para>
/// <example>
/// <code>
/// public static Target Username    = Target.The("username field").ByLabel("Username");
/// public static Target LoginButton = Target.The("login button").ByRole("button", "Sign in");
/// </code>
/// </example>
/// </remarks>
public sealed class Target
{
    private readonly List<LocatorStrategy> _strategies;

    /// <summary>Human-readable description used in logs and error messages.</summary>
    public string Name { get; }

    /// <summary>The ordered list of locator strategies for this target.</summary>
    public IReadOnlyList<LocatorStrategy> Strategies => _strategies;

    private Target(string name, List<LocatorStrategy> strategies)
    {
        Name = name;
        _strategies = strategies;
    }

    /// <summary>Starts building a target with the given human-readable name.</summary>
    public static Target The(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Target(name, []);
    }

    /// <summary>Adds a CSS selector strategy.</summary>
    public Target ByCss(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        return With(new LocatorStrategy(LocatorStrategyKind.Css, selector));
    }

    /// <summary>Adds an XPath strategy.</summary>
    public Target ByXPath(string xpath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xpath);
        return With(new LocatorStrategy(LocatorStrategyKind.XPath, xpath));
    }

    /// <summary>Adds an ARIA role strategy, optionally with an accessible name.</summary>
    public Target ByRole(string role, string? accessibleName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return With(new LocatorStrategy(LocatorStrategyKind.Role, role, accessibleName));
    }

    /// <summary>Adds a label-text strategy.</summary>
    public Target ByLabel(string labelText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(labelText);
        return With(new LocatorStrategy(LocatorStrategyKind.Label, labelText));
    }

    /// <summary>Adds an HTML id strategy.</summary>
    public Target ById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return With(new LocatorStrategy(LocatorStrategyKind.Id, id));
    }

    /// <summary>Adds a test-id strategy (e.g., data-testid).</summary>
    public Target ByTestId(string testId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);
        return With(new LocatorStrategy(LocatorStrategyKind.TestId, testId));
    }

    /// <summary>Adds a visible-text strategy.</summary>
    public Target ByText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return With(new LocatorStrategy(LocatorStrategyKind.Text, text));
    }

    /// <summary>Adds a placeholder-text strategy.</summary>
    public Target ByPlaceholder(string placeholder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placeholder);
        return With(new LocatorStrategy(LocatorStrategyKind.Placeholder, placeholder));
    }

    /// <summary>Adds an alt-text strategy.</summary>
    public Target ByAltText(string altText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(altText);
        return With(new LocatorStrategy(LocatorStrategyKind.AltText, altText));
    }

    /// <inheritdoc/>
    public override string ToString() => Name;

    private Target With(LocatorStrategy strategy) =>
        new(Name, [.. _strategies, strategy]);
}
