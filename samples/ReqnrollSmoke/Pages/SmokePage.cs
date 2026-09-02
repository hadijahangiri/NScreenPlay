using NScreenplay.Core;

namespace ReqnrollSmoke.Pages;

public static class SmokePage
{
    public static Target Input = Target.The("smoke input").ByTestId("smoke-input");
    public static Target Submit = Target.The("smoke submit").ByTestId("smoke-submit");
    public static Target Result = Target.The("smoke result").ByTestId("smoke-result");
}
