using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Interaction: clicks a target element.
/// </summary>
/// <example>
/// <code>
/// await actor.AttemptsTo(Click.On(LoginPage.LoginButton));
/// </code>
/// </example>
public sealed class Click : IInteraction
{
    private readonly Target _target;

    private Click(Target target) => _target = target;

    /// <summary>Creates a click interaction for the given target.</summary>
    public static Click On(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new Click(target);
    }

    /// <inheritdoc/>
    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        await locator.ClickAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Click on {_target.Name}";
}
