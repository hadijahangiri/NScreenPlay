using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Reqnroll;

namespace NScreenplay.Reqnroll;

internal static class FailureEvidenceCapturer
{
    public static async Task CaptureAsync(ScenarioActor scenarioActor, FeatureContext featureContext, ScenarioContext scenarioContext)
    {
        var options = NScreenplayConfiguration.Options;

        string artifactsDir;
        try
        {
            artifactsDir = ArtifactPathValidator.ResolveAndValidate(options.ArtifactsDirectory);
        }
        catch (ArgumentException)
        {
            // If configured path is invalid, fall back to an in-repo default under ./artifacts
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "nscreenplay-artifacts");
            artifactsDir = Path.GetFullPath(fallback);
        }

        Directory.CreateDirectory(artifactsDir);

        var evidence = new LocalFailureEvidence(null, null, false, false);

        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmssfff");
            var feature = SanitizeFileName(featureContext.FeatureInfo.Title);
            var scenario = SanitizeFileName(scenarioContext.ScenarioInfo.Title);

            // Screenshot
            try
            {
                var page = scenarioActor.Page;
                var pngPath = Path.Combine(artifactsDir, $"{feature}--{scenario}--{timestamp}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = pngPath }).ConfigureAwait(false);
                evidence = evidence with { ScreenshotPath = pngPath, ScreenshotAvailable = true };
            }
            catch
            {
                // swallow
            }

            // Tracing is intentionally omitted here (no tracing/start/stop).
            // We keep only screenshot capture to avoid introducing tracing behavior.
        }
        finally
        {
            try
            {
                var fc = new LocalFailureContext
                {
                    ScenarioTitle = scenarioContext.ScenarioInfo.Title,
                    FeatureTitle = featureContext.FeatureInfo.Title,
                    StepText = string.Empty,
                    TaskName = null,
                    InteractionName = null,
                    TargetName = null,
                    PageUrl = scenarioActor.Page?.Url,
                    ExceptionType = scenarioContext.TestError?.GetType().Name ?? string.Empty,
                    ExceptionMessage = scenarioContext.TestError?.Message ?? string.Empty,
                    StackTraceSummary = scenarioContext.TestError?.StackTrace?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(3).FirstOrDefault(),
                    Evidence = evidence,
                    Timestamp = DateTimeOffset.UtcNow
                };

                var outPath = Path.Combine(artifactsDir, $"failure-{DateTimeOffset.UtcNow:yyyyMMdd_HHmmssfff}.json");
                var json = JsonSerializer.Serialize(fc, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(outPath, json);
            }
            catch
            {
                // swallow
            }
        }
    }

    internal static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            sb.Append(invalid.Contains(c) ? '_' : c);
        return sb.ToString();
    }

    internal sealed record LocalFailureEvidence(string? ScreenshotPath, string? TraceArchivePath, bool ScreenshotAvailable, bool TraceAvailable);

    internal sealed class LocalFailureContext
    {
        public string ScenarioTitle { get; set; } = string.Empty;
        public string FeatureTitle { get; set; } = string.Empty;
        public string StepText { get; set; } = string.Empty;
        public string? TaskName { get; set; }
        public string? InteractionName { get; set; }
        public string? TargetName { get; set; }
        public string? PageUrl { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public string ExceptionMessage { get; set; } = string.Empty;
        public string? StackTraceSummary { get; set; }
        public LocalFailureEvidence Evidence { get; set; } = new(null, null, false, false);
        public DateTimeOffset Timestamp { get; set; }
    }

}
