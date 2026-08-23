using Microsoft.Playwright;
using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Resolves a <see cref="Target"/> into a Playwright <see cref="ILocator"/> using the
/// first strategy that the given page supports.
/// </summary>
/// <remarks>
/// Strategy priority follows the order in which strategies were added to the target.
/// The first recognized strategy wins. Unknown strategy kinds throw
/// <see cref="NotSupportedException"/>.
/// </remarks>
public static class TargetResolver
{
    /// <summary>
    /// Resolves <paramref name="target"/> to a Playwright <see cref="ILocator"/> on the given page.
    /// </summary>
    /// <exception cref="ArgumentException">When the target has no strategies.</exception>
    /// <exception cref="NotSupportedException">When the first strategy kind is not recognized.</exception>
    public static ILocator Resolve(IPage page, Target target)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(target);

        if (target.Strategies.Count == 0)
            throw new ArgumentException(
                $"Target '{target.Name}' has no locator strategies. " +
                "Add at least one strategy (e.g., .ByCss(...), .ByRole(...)).",
                nameof(target));

        var strategy = target.Strategies[0];
        return strategy.Kind switch
        {
            LocatorStrategyKind.Css         => page.Locator(strategy.Value),
            LocatorStrategyKind.XPath       => page.Locator(strategy.Value),
            LocatorStrategyKind.Id          => page.Locator($"#{strategy.Value}"),
            LocatorStrategyKind.Text        => page.GetByText(strategy.Value),
            LocatorStrategyKind.Placeholder => page.GetByPlaceholder(strategy.Value),
            LocatorStrategyKind.AltText     => page.GetByAltText(strategy.Value),
            LocatorStrategyKind.TestId      => page.GetByTestId(strategy.Value),
            LocatorStrategyKind.Label       => page.GetByLabel(strategy.Value),
            LocatorStrategyKind.Role        => ResolveByRole(page, strategy),
            _ => throw new NotSupportedException(
                $"Locator strategy '{strategy.Kind}' is not supported by the Playwright adapter.")
        };
    }

    private static ILocator ResolveByRole(IPage page, LocatorStrategy strategy)
    {
        // Playwright's GetByRole requires an AriaRole enum value
        if (!Enum.TryParse<AriaRole>(strategy.Value, ignoreCase: true, out var role))
            throw new ArgumentException(
                $"'{strategy.Value}' is not a valid ARIA role. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<AriaRole>())}");

        var options = strategy.Qualifier is not null
            ? new PageGetByRoleOptions { Name = strategy.Qualifier }
            : null;

        return options is not null
            ? page.GetByRole(role, options)
            : page.GetByRole(role);
    }
}
