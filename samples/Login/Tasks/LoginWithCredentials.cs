using Login.Pages;
using NScreenplay.Core;
using NScreenplay.Playwright;

namespace Login.Tasks;

/// <summary>
/// Logs the actor into the application.
/// Composes three atomic interactions: enter username, enter password, click login.
/// </summary>
public sealed class LoginWithCredentials : ITask
{
    private readonly string _username;
    private readonly string _password;

    private LoginWithCredentials(string username, string password)
    {
        _username = username;
        _password = password;
    }

    /// <summary>Creates a login task with the supplied credentials.</summary>
    public static LoginWithCredentials Using(string username, string password) =>
        new(username, password);

    /// <inheritdoc/>
    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        await actor.AttemptsTo(Enter.TheValue(_username).Into(LoginPage.Username), cancellationToken);
        await actor.AttemptsTo(Enter.TheValue(_password).Into(LoginPage.Password), cancellationToken);
        await actor.AttemptsTo(Click.On(LoginPage.LoginButton), cancellationToken);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Log in as '{_username}'";
}
