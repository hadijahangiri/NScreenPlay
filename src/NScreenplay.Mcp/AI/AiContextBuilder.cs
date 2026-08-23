using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Models;

namespace NScreenplay.Mcp.AI;

/// <summary>
/// Compact AI context builder — packages framework metadata for AI consumption.
/// Keeps context small: no source code dumps, only structured metadata.
/// </summary>
public sealed class AiContextBuilder
{
    private readonly ComponentDiscovery _discovery;
    private readonly SkillLoader _skillLoader;

    public AiContextBuilder(ComponentDiscovery discovery, SkillLoader skillLoader)
    {
        _discovery = discovery;
        _skillLoader = skillLoader;
    }

    /// <summary>
    /// Builds a compact, structured context suitable for inclusion in an AI prompt.
    /// Content is DATA for AI consumption — it does not execute any discovered code.
    /// </summary>
    public AiContext Build()
    {
        return new AiContext(
            FrameworkVersion: "0.1.0",
            AvailableTasks: _discovery.DiscoverTasks().Select(t => t.Name).ToList(),
            AvailableTargets: _discovery.DiscoverTargets()
                .Select(t => $"{t.DeclaringType}.{t.Name} ({t.HumanReadableName})").ToList(),
            AvailableInteractions: _discovery.DiscoverInteractions().Select(i => i.Name).ToList(),
            AvailableQuestions: _discovery.DiscoverQuestions()
                .Select(q => $"{q.Name}<{q.AnswerType}>").ToList(),
            AvailableSkills: _skillLoader.ListSkills().Select(s => s.Name).ToList(),
            ArchitectureRules: [
                "Actor owns Abilities and executes Tasks, Interactions, Questions, Consequences.",
                "Tasks are business-level (Login.WithCredentials), not UI-level (FillUsername).",
                "Interactions are atomic: Click, Enter, Navigate, Select, Check.",
                "Targets are semantic: prefer ByTestId > ByRole > ByLabel > ByCss.",
                "Questions read state and never mutate it.",
                "Step Definitions must be thin: one line per step calling a Task or Consequence.",
                "No raw Playwright APIs in Tasks or Step Definitions.",
                "No Thread.Sleep or Task.Delay in tests.",
                "Prefer reusing existing components over creating duplicates.",
            ]);
    }
}

/// <summary>Compact AI context snapshot.</summary>
public sealed record AiContext(
    string FrameworkVersion,
    IReadOnlyList<string> AvailableTasks,
    IReadOnlyList<string> AvailableTargets,
    IReadOnlyList<string> AvailableInteractions,
    IReadOnlyList<string> AvailableQuestions,
    IReadOnlyList<string> AvailableSkills,
    IReadOnlyList<string> ArchitectureRules);
