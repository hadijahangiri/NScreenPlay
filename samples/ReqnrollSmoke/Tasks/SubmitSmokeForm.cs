using NScreenplay.Core;
using NScreenplay.Playwright;
using ReqnrollSmoke.Pages;

namespace ReqnrollSmoke.Tasks;

public sealed class SubmitSmokeForm : ITask
{
    private readonly string _value;

    private SubmitSmokeForm(string value)
    {
        _value = value;
    }

    public static SubmitSmokeForm With(string value) => new(value);

    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        await actor.AttemptsTo(Enter.TheValue(_value).Into(SmokePage.Input), cancellationToken);
        await actor.AttemptsTo(Click.On(SmokePage.Submit), cancellationToken);
    }
}
