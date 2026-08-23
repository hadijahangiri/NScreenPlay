using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Models;

namespace NScreenplay.Mcp.Tests;

public class FailureAnalyzerTests
{
    private readonly FailureAnalyzer _analyzer = new();

    private static FailureInput MakeInput(
        string exceptionType,
        string exceptionMessage,
        string? targetName = null,
        string? taskName = null) =>
        new(
            ScenarioTitle: "Successful login",
            StepText: "When the user logs in with valid credentials",
            TaskName: taskName,
            InteractionName: "Click",
            TargetName: targetName,
            PageUrl: "https://localhost/login",
            ExceptionType: exceptionType,
            ExceptionMessage: exceptionMessage,
            StackTraceSummary: null,
            ScreenshotAvailable: false);

    [Fact]
    public void Analyze_PlaywrightTimeoutWithLocator_ClassifiesAsSelectorFailure()
    {
        var input = MakeInput("PlaywrightException",
            "Timeout 30000ms exceeded while waiting for locator('data-testid=login-btn') to be visible.");
        var result = _analyzer.Analyze(input);
        Assert.Equal("SelectorFailure", result.Category);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    [Fact]
    public void Analyze_MissingAbilityException_ClassifiesAsInfrastructure()
    {
        var input = MakeInput("MissingAbilityException",
            "Actor 'Alice' does not have the ability 'BrowseTheWeb'.");
        var result = _analyzer.Analyze(input);
        Assert.Equal("InfrastructureFailure", result.Category);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    [Fact]
    public void Analyze_BrowserManagerKeyNotFound_ClassifiesAsInfrastructure()
    {
        var input = MakeInput("KeyNotFoundException",
            "The given key 'NScreenplay.Reqnroll.BrowserManager' was not present in the dictionary.");
        var result = _analyzer.Analyze(input);
        Assert.Equal("InfrastructureFailure", result.Category);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    [Fact]
    public void Analyze_PlaywrightExecutableMissing_ClassifiesAsInfrastructure()
    {
        var input = MakeInput("PlaywrightException",
            "Executable doesn't exist at C:\\ms-playwright\\chromium_headless_shell-1148\\headless_shell.exe");
        var result = _analyzer.Analyze(input);
        Assert.Equal("InfrastructureFailure", result.Category);
    }

    [Fact]
    public void Analyze_InvalidOperationWithTarget_ClassifiesAsTestOrApp()
    {
        var input = MakeInput("InvalidOperationException",
            "Expected dashboard heading to be visible, but it was not.",
            targetName: "DashboardPage.Heading");
        var result = _analyzer.Analyze(input);
        Assert.Equal("TestLogicOrApplicationFailure", result.Category);
    }

    [Fact]
    public void Analyze_UnknownException_ClassifiesAsUnknownWithLowConfidence()
    {
        var input = MakeInput("WeirdException", "Something very unexpected happened.");
        var result = _analyzer.Analyze(input);
        Assert.Equal("Unknown", result.Category);
        Assert.Equal(ConfidenceLevel.Low, result.Confidence);
    }

    [Fact]
    public void Analyze_AlwaysIncludesDoNotDoList()
    {
        var input = MakeInput("PlaywrightException", "some error");
        var result = _analyzer.Analyze(input);
        Assert.NotEmpty(result.DoNotDo);
        Assert.Contains(result.DoNotDo, s => s.Contains("Thread.Sleep"));
        Assert.Contains(result.DoNotDo, s => s.Contains("human review"));
    }

    [Fact]
    public void Analyze_ThrowsForNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.Analyze(null!));
    }

    [Fact]
    public void Analyze_EvidenceContainsScenarioAndStep()
    {
        var input = MakeInput("PlaywrightException", "timeout");
        var result = _analyzer.Analyze(input);
        Assert.Contains("Successful login", result.Evidence);
        Assert.Contains("When the user logs in", result.Evidence);
    }
}
