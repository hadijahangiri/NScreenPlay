using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NScreenplay.Mcp.AI;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Healing;
using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Planning;
using NScreenplay.Mcp.Prompts;
using NScreenplay.Mcp.Resources;
using NScreenplay.Mcp.Tools;
using System.Reflection;

var skillsPath = Environment.GetEnvironmentVariable("NSCREENPLAY_SKILLS_PATH")
    ?? FindSkillsDirectory()
    ?? Path.Combine(AppContext.BaseDirectory, "skills");

var scanAssemblies = LoadScanAssemblies();
var workspaceRoot = Environment.GetEnvironmentVariable("NSCREENPLAY_WORKSPACE_ROOT")
    ?? Directory.GetCurrentDirectory();

var builder = Host.CreateApplicationBuilder(args);

// MCP uses stdio — suppress info logs that would corrupt the stream
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Warning);

builder.Services.AddSingleton(new SkillLoader(skillsPath));
builder.Services.AddSingleton(new ComponentDiscovery(scanAssemblies));
builder.Services.AddSingleton<FailureAnalyzer>();
builder.Services.AddSingleton<RequirementAnalyzer>();
builder.Services.AddSingleton<TestPlanGenerator>();
builder.Services.AddSingleton<AiContextBuilder>();
builder.Services.AddSingleton<AdoptionPlanner>();
builder.Services.AddSingleton(new AdoptionApplier(workspaceRoot));
builder.Services.AddSingleton<NScreenplayTools>();
builder.Services.AddSingleton<PlanningTools>();
builder.Services.AddSingleton<NScreenplayResources>();
builder.Services.AddSingleton<NScreenplayPrompts>();

// Healing services
builder.Services.AddSingleton(new ProjectAnalyzer(workspaceRoot, skillsPath));
builder.Services.AddSingleton(new FileSafetyValidator(workspaceRoot));
builder.Services.AddSingleton<ProposalStore>();
builder.Services.AddSingleton<ProposalApplicator>();
builder.Services.AddSingleton(new HealingEngine(workspaceRoot));
builder.Services.AddSingleton<HealingTools>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<NScreenplayTools>()
    .WithTools<PlanningTools>()
    .WithTools<HealingTools>()
    .WithResources<NScreenplayResources>()
    .WithPrompts<NScreenplayPrompts>();

var app = builder.Build();
await app.RunAsync();

static string? FindSkillsDirectory()
{
    var dir = AppContext.BaseDirectory;
    for (var i = 0; i < 10; i++)
    {
        var candidate = Path.Combine(dir, "skills");
        if (Directory.Exists(candidate) && Directory.GetDirectories(candidate).Length > 0)
            return candidate;
        var parent = Path.GetDirectoryName(dir);
        if (parent is null || parent == dir) break;
        dir = parent;
    }
    return null;
}

static IEnumerable<Assembly> LoadScanAssemblies()
{
    var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
    var envPaths = Environment.GetEnvironmentVariable("NSCREENPLAY_SCAN_ASSEMBLIES");
    if (string.IsNullOrWhiteSpace(envPaths)) return assemblies;

    foreach (var path in envPaths.Split(';', StringSplitOptions.RemoveEmptyEntries))
    {
        var trimmed = path.Trim();
        if (!File.Exists(trimmed) || !trimmed.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            continue;
        try { assemblies.Add(Assembly.LoadFrom(trimmed)); }
        catch { /* skip assemblies that cannot be loaded */ }
    }
    return assemblies;
}
