using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Question: reads the visible inner text of a target element.
/// </summary>
/// <example>
/// <code>
/// var title = await actor.AsksFor(Text.Of(Dashboard.WelcomeHeading));
/// </code>
/// </example>
public sealed class Text : IQuestion<string>
{
    private readonly Target _target;

    private Text(Target target) => _target = target;

    /// <summary>Creates a question that reads the text content of the given target.</summary>
    public static Text Of(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new Text(target);
    }

    /// <inheritdoc/>
    public async Task<string> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        return await locator.InnerTextAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Text of {_target.Name}";
}
