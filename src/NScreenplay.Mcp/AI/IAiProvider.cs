namespace NScreenplay.Mcp.AI;

/// <summary>
/// Optional abstraction for an LLM provider. The framework works fully without one.
/// When no provider is configured, all capabilities operate deterministically.
/// </summary>
public interface IAiProvider
{
    /// <summary>Sends a prompt and returns the completion text.</summary>
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}

/// <summary>
/// Null implementation — used when no LLM provider is configured.
/// Returns a message indicating no AI provider is available.
/// </summary>
internal sealed class NullAiProvider : IAiProvider
{
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default) =>
        Task.FromResult("[No AI provider configured. Operating in deterministic mode.]");
}
