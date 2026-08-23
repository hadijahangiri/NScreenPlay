namespace NScreenplay.Core;

/// <summary>
/// Represents a read-only query about the state of the system under test.
/// </summary>
/// <typeparam name="TAnswer">The type of the answer returned by the question.</typeparam>
/// <remarks>
/// Questions must not mutate state. They only observe.
/// Example: Text.Of(LoginPage.Username), PageTitle.Current()
/// </remarks>
public interface IQuestion<TAnswer>
{
    /// <summary>Retrieves the answer in the context of the given actor.</summary>
    Task<TAnswer> AnsweredBy(Actor actor, CancellationToken cancellationToken = default);
}
