namespace ReqnrollSmoke.Support;

public static class LifecycleProbe
{
    public static bool BeforeScenarioInitialized { get; set; }
    public static bool AfterScenarioDisposalObserved { get; set; }

    public static void Reset()
    {
        BeforeScenarioInitialized = false;
        AfterScenarioDisposalObserved = false;
    }
}
