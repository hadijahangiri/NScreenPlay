using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Interaction: selects an option in a &lt;select&gt; element by visible label.
/// </summary>
/// <example>
/// <code>
/// await actor.AttemptsTo(Select.TheOption("Canada").From(CheckoutPage.CountryDropdown));
/// </code>
/// </example>
public sealed class Select : IInteraction
{
    private readonly string _optionLabel;
    private Target? _target;

    private Select(string optionLabel) => _optionLabel = optionLabel;

    /// <summary>Starts building a Select interaction with the given visible option text.</summary>
    public static Select TheOption(string optionLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionLabel);
        return new Select(optionLabel);
    }

    /// <summary>Specifies the &lt;select&gt; target element.</summary>
    public Select From(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _target = target;
        return this;
    }

    /// <inheritdoc/>
    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        if (_target is null)
            throw new InvalidOperationException(
                "Select interaction has no target. Call .From(target) before executing.");

        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        await locator.SelectOptionAsync(_optionLabel).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Select '{_optionLabel}' from {_target?.Name ?? "unknown"}";
}
