using NScreenplay.Mcp.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;

namespace NScreenplay.Mcp.Adoption;

/// <summary>
/// Applies a previously generated adoption plan to a .NET project in a safe, idempotent way.
/// The apply phase is deliberately limited to the plan's explicit package and project instructions.
/// </summary>
public sealed class AdoptionApplier
{
    private static readonly Regex PackageIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedPackages =
    [
        "NScreenplay.Core",
        "NScreenplay.Playwright",
        "NScreenplay.Reqnroll"
    ];
    private readonly string _workspaceRoot;

    public AdoptionApplier(string workspaceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public AdoptionApplyResult Apply(AdoptionPlan plan, string projectPath, bool dryRun = false)
    {
        if (plan is null)
            return new AdoptionApplyResult("ValidationFailed", [], ["Plan is required."], [], projectPath);

        var projectErrors = ValidatePlan(plan, projectPath);
        if (projectErrors.Count > 0)
            return new AdoptionApplyResult("ValidationFailed", [], projectErrors, [], projectPath);

        string resolvedPlanPath;
        string resolvedTargetPath;
        try
        {
            resolvedPlanPath = ResolveProjectPath(plan.ProjectPath);
            resolvedTargetPath = ResolveProjectPath(projectPath);
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or InvalidOperationException or IOException)
        {
            return new AdoptionApplyResult("ValidationFailed", [], [ex.Message], [], null);
        }

        if (!InputValidator.IsPathWithinRoot(resolvedPlanPath, _workspaceRoot))
            return new AdoptionApplyResult("PreconditionFailed", [], ["Plan project path is outside the allowed workspace root."], [], resolvedPlanPath);

        if (!InputValidator.IsPathWithinRoot(resolvedTargetPath, _workspaceRoot))
            return new AdoptionApplyResult("PreconditionFailed", [], ["Project path is outside the allowed workspace root."], [], resolvedTargetPath);

        if (ContainsReparsePoint(resolvedPlanPath, _workspaceRoot) || ContainsReparsePoint(resolvedTargetPath, _workspaceRoot))
            return new AdoptionApplyResult("PreconditionFailed", [], ["Project path traverses a symlink/junction (reparse point), which is not allowed for Phase C apply."], [], resolvedTargetPath);

        if (!string.Equals(resolvedPlanPath, resolvedTargetPath, StringComparison.OrdinalIgnoreCase))
            return new AdoptionApplyResult("Conflict", [], [$"Project path '{projectPath}' does not match the plan's project path '{plan.ProjectPath}'."], [], resolvedTargetPath);

        if (!File.Exists(resolvedTargetPath) || !resolvedTargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return new AdoptionApplyResult("PreconditionFailed", [], [$"Target project '{resolvedTargetPath}' is not a valid .csproj file."], [], resolvedTargetPath);

        var operations = new List<string>();
        var warnings = new List<string>();
        var normalizedPackages = plan.RecommendedPackages
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        XDocument xml;
        XElement project;
        try
        {
            xml = XDocument.Load(resolvedTargetPath, LoadOptions.PreserveWhitespace);
            project = xml.Root ?? throw new InvalidOperationException($"Project file '{resolvedTargetPath}' is missing a root element.");
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return new AdoptionApplyResult("ValidationFailed", [], [ex.Message], [], resolvedTargetPath);
        }

        var ns = project.Name.Namespace;
        var packageReferenceName = ns + "PackageReference";
        var itemGroupName = ns + "ItemGroup";

        foreach (var trimmed in normalizedPackages)
        {
            if (!AllowedPackages.Contains(trimmed))
                return new AdoptionApplyResult("ValidationFailed", operations, [$"Unsupported package '{trimmed}'. Allowed packages: {string.Join(", ", AllowedPackages)}."], warnings, resolvedTargetPath);

            if (!PackageIdPattern.IsMatch(trimmed))
                return new AdoptionApplyResult("ValidationFailed", operations, [$"Invalid package id '{trimmed}'."], warnings, resolvedTargetPath);

            var existing = project
                .Descendants(packageReferenceName)
                .FirstOrDefault(e => string.Equals(e.Attribute("Include")?.Value, trimmed, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(e.Attribute("Update")?.Value, trimmed, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                warnings.Add($"Package '{trimmed}' already exists; no change applied.");
                continue;
            }

            if (dryRun)
            {
                operations.Add($"Would add PackageReference '{trimmed}'.");
                continue;
            }

            var itemGroup = project
                .Elements(itemGroupName)
                .FirstOrDefault(group => group.Elements(packageReferenceName).Any())
                ?? new XElement(itemGroupName);

            if (!project.Elements(itemGroupName).Any(g => g == itemGroup))
                project.Add(itemGroup);

            itemGroup.Add(new XElement(packageReferenceName, new XAttribute("Include", trimmed)));
            operations.Add($"Added PackageReference '{trimmed}'.");
        }

        if (dryRun)
            return new AdoptionApplyResult("DryRun", operations, [], warnings, resolvedTargetPath);

        if (operations.Count == 0)
        {
            warnings.Add("No changes were required; the project is already in the requested package state.");
            return new AdoptionApplyResult("Success", operations, [], warnings, resolvedTargetPath);
        }

        var tempPath = resolvedTargetPath + ".nscreenplay.apply.tmp";
        try
        {
            using (var writer = XmlWriter.Create(tempPath, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                OmitXmlDeclaration = false,
            }))
            {
                xml.Save(writer);
            }

            File.Move(tempPath, resolvedTargetPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }

        return new AdoptionApplyResult("Success", operations, [], warnings, resolvedTargetPath);
    }

    private List<string> ValidatePlan(AdoptionPlan plan, string projectPath)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(plan.ProjectPath))
            errors.Add("Plan is missing ProjectPath.");

        if (string.IsNullOrWhiteSpace(projectPath))
            errors.Add("Project path is required.");

        if (plan.RecommendedPackages is null)
            errors.Add("Plan is missing RecommendedPackages.");

        if (plan.RecommendedPackages is not null && plan.RecommendedPackages.Any(x => string.IsNullOrWhiteSpace(x)))
            errors.Add("Plan contains an empty recommended package name.");

        if (plan.RecommendedPackages is not null && plan.RecommendedPackages.Count == 0)
            errors.Add("Plan contains no package changes to apply.");

        if (plan.Steps is null || plan.Steps.Count == 0)
            errors.Add("Plan contains no steps.");

        var packageStepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (plan.Steps is not null)
        {
            foreach (var stepId in plan.Steps
                .Where(s => string.Equals(s.Category, "package", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Id))
            {
                packageStepIds.Add(stepId);
            }
        }

        if (plan.RecommendedPackages is not null && plan.RecommendedPackages.Count > 0 && packageStepIds.Count == 0)
            errors.Add("Plan contains package changes but no package-category steps.");

        return errors;
    }

    private string ResolveProjectPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return string.Empty;

        var candidate = projectPath.Trim();
        var fullPath = Path.IsPathRooted(candidate)
            ? Path.GetFullPath(candidate)
            : Path.GetFullPath(Path.Combine(_workspaceRoot, candidate));

        if (Directory.Exists(fullPath))
        {
            var files = Directory.GetFiles(fullPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 1)
                return Path.GetFullPath(files[0]);
            if (files.Length > 1)
                throw new InvalidOperationException($"Directory '{fullPath}' contains multiple .csproj files; specify one explicitly.");
        }

        return fullPath;
    }

    private static bool ContainsReparsePoint(string fullPath, string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(workspaceRoot))
            return false;

        var normalizedRoot = Path.GetFullPath(workspaceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath;
        var current = Path.GetFullPath(candidate);

        if (!InputValidator.IsPathWithinRoot(current, normalizedRoot))
            return true;

        while (current.Length >= normalizedRoot.Length)
        {
            if (Directory.Exists(current))
            {
                var attributes = new DirectoryInfo(current).Attributes;
                if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    return true;
            }

            if (string.Equals(current, normalizedRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                break;

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.Ordinal))
                break;
            current = parent;
        }

        return false;
    }
}