using ModelContextProtocol.Server;
using NScreenplay.Mcp.AI;
using NScreenplay.Mcp.Discovery;
using System.ComponentModel;
using System.Text.Json;

namespace NScreenplay.Mcp.Resources;

/// <summary>
/// MCP Resources exposing NScreenplay framework information as stable, addressable URIs.
/// All resources are read-only. Content is structured data â€” not executable code.
/// </summary>
[McpServerResourceType]
public sealed class NScreenplayResources
{
    private readonly ComponentDiscovery _discovery;
    private readonly SkillLoader _skillLoader;
    private readonly AiContextBuilder _contextBuilder;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public NScreenplayResources(
        ComponentDiscovery discovery,
        SkillLoader skillLoader,
        AiContextBuilder contextBuilder)
    {
        _discovery = discovery;
        _skillLoader = skillLoader;
        _contextBuilder = contextBuilder;
    }

    [McpServerResource(UriTemplate = "nscreenplay://framework", Name = "Framework Info", MimeType = "application/json")]
    [Description("NScreenplay framework version, modules, and capabilities.")]
    public string GetFrameworkResource() =>
        JsonSerializer.Serialize(new
        {
            name = "NScreenplay",
            version = "0.1.0",
            description = "AI-native Screenplay Test Automation Framework for .NET",
            modules = new[] { "NScreenplay.Core", "NScreenplay.Playwright", "NScreenplay.Reqnroll", "NScreenplay.Mcp" },
            coreAbstractions = new[] { "Actor", "Ability", "Task", "Interaction", "Target", "Question", "Consequence" },
            approvalBoundary = new
            {
                aiCanDo = new[] { "DISCOVER", "ANALYZE", "PLAN", "PROPOSE" },
                aiCannotDo = new[] { "WRITE", "EXECUTE_SHELL", "COMMIT", "MODIFY_TESTS" },
                note = "All modifications require explicit human approval."
            }
        }, JsonOpts);

    [McpServerResource(UriTemplate = "nscreenplay://architecture", Name = "Architecture", MimeType = "application/json")]
    [Description("NScreenplay dependency architecture and core principles.")]
    public string GetArchitectureResource() =>
        JsonSerializer.Serialize(new
        {
            dependencyDirection = new
            {
                rule = "Integrations depend on Core. Core never depends on integrations.",
                graph = "Reqnroll.Reqnroll â†’ Core â† Playwright | MCP â†’ Core"
            },
            coreIndependence = "NScreenplay.Core has zero third-party dependencies.",
            asyncModel = "All execution APIs are async-first with CancellationToken.",
            stateModel = "No static Actor state. No AsyncLocal. Each scenario gets its own Actor.",
            targetModel = "Targets are adapter-neutral semantic descriptors. Playwright translates them.",
            selectorPriority = new[] { "ByTestId (most stable)", "ByRole", "ByLabel", "ByText", "ByCss", "ByXPath (last resort)" }
        }, JsonOpts);

    [McpServerResource(UriTemplate = "nscreenplay://skills", Name = "Available Skills", MimeType = "application/json")]
    [Description("List of available NScreenplay AI agent skills.")]
    public string GetSkillsResource()
    {
        var skills = _skillLoader.ListSkills();
        return JsonSerializer.Serialize(skills.Select(s => new
        {
            s.Name,
            s.FirstHeading,
            uri = $"nscreenplay://skills/{s.Name}"
        }), JsonOpts);
    }

    [McpServerResource(UriTemplate = "nscreenplay://tasks", Name = "Discovered Tasks", MimeType = "application/json")]
    [Description("All NScreenplay ITask implementations discovered in loaded assemblies.")]
    public string GetTasksResource() =>
        JsonSerializer.Serialize(_discovery.DiscoverTasks(), JsonOpts);

    [McpServerResource(UriTemplate = "nscreenplay://targets", Name = "Discovered Targets", MimeType = "application/json")]
    [Description("All NScreenplay static Target fields discovered in loaded assemblies.")]
    public string GetTargetsResource() =>
        JsonSerializer.Serialize(_discovery.DiscoverTargets(), JsonOpts);

    [McpServerResource(UriTemplate = "nscreenplay://interactions", Name = "Discovered Interactions", MimeType = "application/json")]
    [Description("All NScreenplay IInteraction implementations discovered in loaded assemblies.")]
    public string GetInteractionsResource() =>
        JsonSerializer.Serialize(_discovery.DiscoverInteractions(), JsonOpts);

    [McpServerResource(UriTemplate = "nscreenplay://questions", Name = "Discovered Questions", MimeType = "application/json")]
    [Description("All NScreenplay IQuestion<T> implementations discovered in loaded assemblies.")]
    public string GetQuestionsResource() =>
        JsonSerializer.Serialize(_discovery.DiscoverQuestions(), JsonOpts);

    [McpServerResource(UriTemplate = "nscreenplay://context", Name = "AI Context", MimeType = "application/json")]
    [Description("Compact AI context with framework metadata, available components, and architecture rules.")]
    public string GetAiContextResource() =>
        JsonSerializer.Serialize(_contextBuilder.Build(), JsonOpts);
}
