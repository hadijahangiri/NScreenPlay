using NScreenplay.Core;

namespace Login.Pages;

/// <summary>Targets on the Login page.</summary>
public static class LoginPage
{
    public static Target Username    = Target.The("username field").ByTestId("username-input");
    public static Target Password    = Target.The("password field").ByTestId("password-input");
    public static Target LoginButton = Target.The("login button").ByTestId("login-button");
    public static Target ErrorMessage = Target.The("login error message").ByTestId("login-error");
}
