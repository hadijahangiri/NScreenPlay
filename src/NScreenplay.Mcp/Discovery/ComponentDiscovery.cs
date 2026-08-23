using NScreenplay.Core;
using NScreenplay.Mcp.Models;
using System.Reflection;

namespace NScreenplay.Mcp.Discovery;

/// <summary>
/// Discovers NScreenplay components (Tasks, Targets, Interactions, Questions)
/// from a set of assemblies via reflection.
/// </summary>
public sealed class ComponentDiscovery
{
    private readonly IReadOnlyList<Assembly> _assemblies;

    public ComponentDiscovery(IEnumerable<Assembly> assemblies)
    {
        _assemblies = assemblies.ToList();
    }

    /// <summary>Returns all concrete (non-abstract, non-interface) types that implement ITask.</summary>
    public IReadOnlyList<DiscoveredTask> DiscoverTasks()
    {
        return DiscoverTypes<ITask>()
            .Select(t => new DiscoveredTask(
                Name: t.Name,
                FullTypeName: t.FullName ?? t.Name,
                Assembly: t.Assembly.GetName().Name ?? "unknown",
                Description: GetDescription(t)))
            .OrderBy(t => t.Name)
            .ToList();
    }

    /// <summary>Returns all concrete types that implement IInteraction.</summary>
    public IReadOnlyList<DiscoveredInteraction> DiscoverInteractions()
    {
        return DiscoverTypes<IInteraction>()
            .Select(t => new DiscoveredInteraction(
                Name: t.Name,
                FullTypeName: t.FullName ?? t.Name,
                Assembly: t.Assembly.GetName().Name ?? "unknown"))
            .OrderBy(i => i.Name)
            .ToList();
    }

    /// <summary>Returns all concrete types that implement IQuestion&lt;T&gt;.</summary>
    public IReadOnlyList<DiscoveredQuestion> DiscoverQuestions()
    {
        var questionInterface = typeof(IQuestion<>);
        return _assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == questionInterface))
            .Select(t =>
            {
                var iface = t.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == questionInterface);
                var answerType = iface.GetGenericArguments()[0].Name;
                return new DiscoveredQuestion(
                    Name: t.Name,
                    FullTypeName: t.FullName ?? t.Name,
                    AnswerType: answerType,
                    Assembly: t.Assembly.GetName().Name ?? "unknown");
            })
            .OrderBy(q => q.Name)
            .ToList();
    }

    /// <summary>Returns all public static Target fields across all types in the assemblies.</summary>
    public IReadOnlyList<DiscoveredTarget> DiscoverTargets()
    {
        var results = new List<DiscoveredTarget>();
        foreach (var assembly in _assemblies)
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(Target));

                foreach (var field in fields)
                {
                    try
                    {
                        if (field.GetValue(null) is Target target)
                        {
                            results.Add(new DiscoveredTarget(
                                Name: field.Name,
                                HumanReadableName: target.Name,
                                DeclaringType: type.Name,
                                Strategies: target.Strategies
                                    .Select(s => new DiscoveredStrategy(
                                        s.Kind.ToString(), s.Value, s.Qualifier))
                                    .ToList()));
                        }
                    }
                    catch
                    {
                        // skip fields that throw on access
                    }
                }
            }
        }
        return results.OrderBy(t => t.DeclaringType).ThenBy(t => t.Name).ToList();
    }

    private IEnumerable<Type> DiscoverTypes<T>() =>
        _assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(T).IsAssignableFrom(t));

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetExportedTypes(); }
        catch { return []; }
    }

    private static string? GetDescription(Type type)
    {
        // Read from XML doc summary would require loading .xml file; return null for now
        return null;
    }
}
