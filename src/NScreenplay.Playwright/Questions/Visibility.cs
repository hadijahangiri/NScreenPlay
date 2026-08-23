using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Question: checks whether a target element is currently visible on the page.
/// </summary>
/// <example>
/// <code>
/// bool visible = await actor.AsksFor(Visibility.Of(Dashboard.Header));
/// </code>
/// </example>
public sealed class Visibility : IQuestion<bool>
{
    private readonly Target _target;

    private Visibility(Target target) => _target = target;

    /// <summary>Creates a question that checks whether the given target is visible.</summary>
    public static Visibility Of(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new Visibility(target);
    }

    /// <inheritdoc/>
    public async Task<bool> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        return await locator.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Visibility of {_target.Name}";
}
