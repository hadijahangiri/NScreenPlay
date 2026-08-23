namespace NScreenplay.Core;

/// <summary>
/// Represents a verifiable expectation about the state of the system under test.
/// </summary>
/// <remarks>
/// Consequences must throw a meaningful exception when the expectation fails.
/// They are not tied to any specific assertion library.
/// Example: See.That(Dashboard.IsDisplayed()), Ensure.That(StatusCode.Is(200))
/// </remarks>
public interface IConsequence
{
    /// <summary>
    /// Evaluates the expectation in the context of the given actor.
    /// Throws when the expectation is not satisfied.
    /// </summary>
    Task EvaluateAs(Actor actor, CancellationToken cancellationToken = default);
}
