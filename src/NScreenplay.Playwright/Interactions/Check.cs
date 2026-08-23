using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Interaction: checks or unchecks a checkbox or radio button.
/// </summary>
/// <example>
/// <code>
/// await actor.AttemptsTo(Check.The(SignupPage.TermsCheckbox));
/// await actor.AttemptsTo(Uncheck.The(SettingsPage.EmailNotifications));
/// </code>
/// </example>
public sealed class Check : IInteraction
{
    private readonly Target _target;
    private readonly bool _checked;

    private Check(Target target, bool @checked)
    {
        _target = target;
        _checked = @checked;
    }

    /// <summary>Creates an interaction that checks the given target.</summary>
    public static Check The(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new Check(target, @checked: true);
    }

    /// <summary>Creates an interaction that unchecks the given target.</summary>
    public static Check Not(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return new Check(target, @checked: false);
    }

    /// <inheritdoc/>
    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        if (_checked)
            await locator.CheckAsync().ConfigureAwait(false);
        else
            await locator.UncheckAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{(_checked ? "Check" : "Uncheck")} {_target.Name}";
}
