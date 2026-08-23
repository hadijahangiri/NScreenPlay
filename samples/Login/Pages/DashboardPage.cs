using NScreenplay.Core;

namespace Login.Pages;

/// <summary>Targets on the Dashboard page.</summary>
public static class DashboardPage
{
    public static Target Heading = Target.The("dashboard heading").ByTestId("dashboard-heading");
}
