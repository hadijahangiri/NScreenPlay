using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Question: reads the value of an HTML input element.
/// </summary>
/// <example>
/// <code>
/// var inputValue = await actor.AsksFor(InputValue.Of(LoginPage.EmailField));
/// </code>
/// </example>
public sealed class InputValue : IQuestion<string>
{
    private readonly Target _target;

    private InputValue(Target target) => _target = target;

    /// <summary>Creates a question that reads the current value of the given input target.</summary>
    public static InputValue Of(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new InputValue(target);
    }

    /// <inheritdoc/>
    public async Task<string> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        return await locator.InputValueAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Input value of {_target.Name}";
}
