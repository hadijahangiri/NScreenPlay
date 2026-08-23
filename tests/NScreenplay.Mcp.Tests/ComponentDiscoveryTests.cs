using NScreenplay.Core;
using NScreenplay.Mcp.Discovery;
using System.Reflection;

namespace NScreenplay.Mcp.Tests;

public class ComponentDiscoveryTests
{
    // Core has no concrete Tasks/Targets/Interactions/Questions — safe as "empty" assembly
    private static ComponentDiscovery EmptyDiscovery() =>
        new([typeof(Actor).Assembly]);

    // Use the test assembly which contains SampleTargets, SampleTask, SampleQuestion
    private static ComponentDiscovery CoreDiscovery() =>
        new([typeof(Actor).Assembly]);

    [Fact]
    public void DiscoverTasks_WithEmptyAssembly_ReturnsEmpty()
    {
        Assert.Empty(EmptyDiscovery().DiscoverTasks());
    }

    [Fact]
    public void DiscoverInteractions_WithEmptyAssembly_ReturnsEmpty()
    {
        Assert.Empty(EmptyDiscovery().DiscoverInteractions());
    }

    [Fact]
    public void DiscoverQuestions_WithEmptyAssembly_ReturnsEmpty()
    {
        Assert.Empty(EmptyDiscovery().DiscoverQuestions());
    }

    [Fact]
    public void DiscoverTargets_WithEmptyAssembly_ReturnsEmpty()
    {
        Assert.Empty(EmptyDiscovery().DiscoverTargets());
    }

    [Fact]
    public void DiscoverTargets_FindsStaticTargetFields()
    {
        var discovery = new ComponentDiscovery([Assembly.GetExecutingAssembly()]);
        var targets = discovery.DiscoverTargets();
        Assert.Contains(targets, t => t.Name == "SampleButton");
    }

    [Fact]
    public void DiscoverTasks_FindsConcreteITaskImplementations()
    {
        var discovery = new ComponentDiscovery([Assembly.GetExecutingAssembly()]);
        var tasks = discovery.DiscoverTasks();
        Assert.Contains(tasks, t => t.Name == "SampleTask");
    }

    [Fact]
    public void DiscoverTasks_ExcludesInterfaces()
    {
        var discovery = new ComponentDiscovery([typeof(Actor).Assembly]);
        var tasks = discovery.DiscoverTasks();
        Assert.DoesNotContain(tasks, t => t.Name == "ITask");
        Assert.DoesNotContain(tasks, t => t.Name == "IInteraction");
    }

    [Fact]
    public void DiscoverQuestions_FindsIQuestionImplementations()
    {
        var discovery = new ComponentDiscovery([Assembly.GetExecutingAssembly()]);
        var questions = discovery.DiscoverQuestions();
        Assert.Contains(questions, q => q.Name == "SampleQuestion");
        Assert.Contains(questions, q => q.AnswerType == "String");
    }

    [Fact]
    public void DiscoverTargets_IncludesLocatorStrategies()
    {
        var discovery = new ComponentDiscovery([Assembly.GetExecutingAssembly()]);
        var targets = discovery.DiscoverTargets();
        var btn = targets.FirstOrDefault(t => t.Name == "SampleButton");
        Assert.NotNull(btn);
        Assert.NotEmpty(btn.Strategies);
        Assert.Equal("Css", btn.Strategies[0].Kind);
    }

    // ── Test doubles ─────────────────────────────────────────────────────────

    public static class SampleTargets
    {
        public static Target SampleButton = Target.The("sample button").ByCss(".sample");
    }

    public sealed class SampleTask : ITask
    {
        public Task PerformAs(Actor actor, CancellationToken ct = default) => Task.CompletedTask;
    }

    public sealed class SampleQuestion : IQuestion<string>
    {
        public Task<string> AnsweredBy(Actor actor, CancellationToken ct = default) =>
            Task.FromResult("answer");
    }
}
