using ModelContextProtocol.Server;
using NScreenplay.Core;
using NScreenplay.Mcp.Prompts;
using NScreenplay.Mcp.Resources;
using NScreenplay.Mcp.Tools;
using NScreenplay.Playwright;
using NScreenplay.Reqnroll;
using System.Reflection;

namespace NScreenplay.Mcp.Tests;

public sealed class ApiCompatibilityContractTests
{
    [Fact]
    public void McpTools_ExposeExpectedStableToolNames()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "nscreenplay_get_framework_info",
            "nscreenplay_list_tasks",
            "nscreenplay_list_targets",
            "nscreenplay_list_interactions",
            "nscreenplay_list_questions",
            "nscreenplay_list_skills",
            "nscreenplay_get_skill",
            "nscreenplay_analyze_failure",
            "nscreenplay_analyze_project",
            "nscreenplay_create_adoption_plan",
            "nscreenplay_apply_adoption_plan",
            "nscreenplay_analyze_requirement",
            "nscreenplay_create_test_plan",
            "nscreenplay_get_failure_context",
            "nscreenplay_get_fix_proposal",
            "nscreenplay_list_fix_proposals",
            "nscreenplay_reject_fix_proposal",
            "nscreenplay_approve_fix_proposal",
            "nscreenplay_apply_fix_proposal",
            "nscreenplay_get_audit_log"
        };

        var actual = GetToolNames(typeof(NScreenplayTools), typeof(PlanningTools), typeof(HealingTools));

        Assert.Equal(expected.Count, actual.Count);
        foreach (var name in expected)
            Assert.Contains(name, actual);
    }

    [Fact]
    public void McpResources_ExposeExpectedStableUris()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "nscreenplay://framework",
            "nscreenplay://adoption-workflow",
            "nscreenplay://architecture",
            "nscreenplay://skills",
            "nscreenplay://tasks",
            "nscreenplay://targets",
            "nscreenplay://interactions",
            "nscreenplay://questions",
            "nscreenplay://context"
        };

        var actual = GetResourceUris(typeof(NScreenplayResources));
        Assert.Equal(expected.Count, actual.Count);
        foreach (var uri in expected)
            Assert.Contains(uri, actual);
    }

    [Fact]
    public void McpPrompts_ExposeExpectedStableNames()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "nscreenplay_create_test"
        };

        var actual = GetPromptNames(typeof(NScreenplayPrompts));
        Assert.Equal(expected.Count, actual.Count);
        foreach (var name in expected)
            Assert.Contains(name, actual);
    }

    [Fact]
    public void CoreAndAdapters_ExposeCriticalPublicApiSurface()
    {
        Assert.NotNull(typeof(Actor).GetMethod(nameof(Actor.Named), BindingFlags.Public | BindingFlags.Static, [typeof(string)]));
        Assert.NotNull(typeof(Actor).GetMethod(nameof(Actor.Can), BindingFlags.Public | BindingFlags.Instance, [typeof(IAbility)]));
        Assert.NotNull(typeof(Actor).GetMethod(nameof(Actor.AttemptsTo), BindingFlags.Public | BindingFlags.Instance, [typeof(IPerformable), typeof(CancellationToken)]));
        Assert.NotNull(typeof(Actor).GetMethod(nameof(Actor.AsksFor), BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(Actor).GetMethod(nameof(Actor.Should), BindingFlags.Public | BindingFlags.Instance, [typeof(IConsequence), typeof(CancellationToken)]));

        Assert.NotNull(typeof(Target).GetMethod(nameof(Target.The), BindingFlags.Public | BindingFlags.Static, [typeof(string)]));
        Assert.NotNull(typeof(Target).GetMethod(nameof(Target.ByTestId), BindingFlags.Public | BindingFlags.Instance, [typeof(string)]));
        Assert.NotNull(typeof(Target).GetMethod(nameof(Target.ByRole), BindingFlags.Public | BindingFlags.Instance, [typeof(string), typeof(string)]));

        Assert.NotNull(typeof(BrowseTheWeb).GetMethod(nameof(BrowseTheWeb.Using), BindingFlags.Public | BindingFlags.Static, [typeof(Microsoft.Playwright.IPage)]));
        Assert.NotNull(typeof(Click).GetMethod(nameof(Click.On), BindingFlags.Public | BindingFlags.Static, [typeof(Target)]));
        Assert.NotNull(typeof(Enter).GetMethod(nameof(Enter.TheValue), BindingFlags.Public | BindingFlags.Static, [typeof(string)]));
        Assert.NotNull(typeof(Select).GetMethod(nameof(Select.TheOption), BindingFlags.Public | BindingFlags.Static, [typeof(string)]));
        Assert.NotNull(typeof(Check).GetMethod(nameof(Check.The), BindingFlags.Public | BindingFlags.Static, [typeof(Target)]));
        Assert.NotNull(typeof(Check).GetMethod(nameof(Check.Not), BindingFlags.Public | BindingFlags.Static, [typeof(Target)]));
        Assert.NotNull(typeof(Text).GetMethod(nameof(Text.Of), BindingFlags.Public | BindingFlags.Static, [typeof(Target)]));
        Assert.NotNull(typeof(Visibility).GetMethod(nameof(Visibility.Of), BindingFlags.Public | BindingFlags.Static, [typeof(Target)]));
        Assert.NotNull(typeof(CurrentUrl).GetMethod(nameof(CurrentUrl.Value), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(PageTitle).GetMethod(nameof(PageTitle.Value), BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(InputValue).GetMethod(nameof(InputValue.Of), BindingFlags.Public | BindingFlags.Static, [typeof(Target)]));

        Assert.NotNull(typeof(NScreenplayHooks));
        Assert.NotNull(typeof(ScenarioActor));
        Assert.NotNull(typeof(BrowserManager));
        Assert.NotNull(typeof(ScenarioActorExtensions).GetMethod(nameof(ScenarioActorExtensions.InitializeFromFeatureBrowserAsync), BindingFlags.Public | BindingFlags.Static));
    }

    private static HashSet<string> GetToolNames(params Type[] types)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attr = method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == nameof(McpServerToolAttribute));
                if (attr is null)
                    continue;

                var nameProp = attr.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                var name = nameProp?.GetValue(attr) as string;
                if (!string.IsNullOrWhiteSpace(name))
                    result.Add(name);
            }
        }

        return result;
    }

    private static HashSet<string> GetResourceUris(Type type)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var attr = method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == nameof(McpServerResourceAttribute));
            if (attr is null)
                continue;

            var uriProp = attr.GetType().GetProperty("UriTemplate", BindingFlags.Public | BindingFlags.Instance);
            var uri = uriProp?.GetValue(attr) as string;
            if (!string.IsNullOrWhiteSpace(uri))
                result.Add(uri);
        }

        return result;
    }

    private static HashSet<string> GetPromptNames(Type type)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var attr = method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == nameof(McpServerPromptAttribute));
            if (attr is null)
                continue;

            var nameProp = attr.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            var name = nameProp?.GetValue(attr) as string;
            if (!string.IsNullOrWhiteSpace(name))
                result.Add(name);
        }

        return result;
    }
}
