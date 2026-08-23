using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Interaction: types text into a target element.
/// </summary>
/// <example>
/// <code>
/// await actor.AttemptsTo(Enter.TheValue("alice@example.com").Into(LoginPage.EmailField));
/// </code>
/// </example>
public sealed class Enter : IInteraction
{
    private readonly string _value;
    private Target? _target;

    private Enter(string value) => _value = value;

    /// <summary>Starts building an Enter interaction with the given text value.</summary>
    public static Enter TheValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Enter(value);
    }

    /// <summary>Specifies the target element to type into.</summary>
    public Enter Into(Target target)
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
                "Enter interaction has no target. Call .Into(target) before executing.");

        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        await locator.FillAsync(_value).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Enter '{_value}' into {_target?.Name ?? "unknown"}";
}
