namespace NScreenplay.Core;

/// <summary>
/// Represents a performable unit of work — either a business-level <see cref="ITask"/>
/// or an atomic <see cref="IInteraction"/> — that an <see cref="Actor"/> can attempt.
/// </summary>
/// <remarks>
/// Both tasks and interactions implement this interface so that <c>Actor.AttemptsTo</c>
/// accepts both without overloading. The distinction (task vs. interaction) is semantic and
/// enforced by convention, not by separate method signatures.
/// </remarks>
public interface IPerformable
{
    /// <summary>Performs the work in the context of the given actor.</summary>
    Task PerformAs(Actor actor, CancellationToken cancellationToken = default);
}
