namespace NScreenplay.Core;

/// <summary>
/// The central entity in the Screenplay pattern. An Actor has abilities and performs tasks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mutability decision:</b> Actor is intentionally mutable with respect to abilities.
/// In a test scenario, an actor may gain abilities progressively (e.g., after login a token
/// ability is added). Immutable copying would require awkward rebinding at every call site.
/// The actor instance is scenario-scoped and should not be shared across parallel scenarios.
/// </para>
/// <para>
/// <b>State isolation:</b> No static or AsyncLocal state exists. Each test creates its own
/// Actor instance. This guarantees parallel-test safety.
/// </para>
/// <example>
/// <code>
/// var actor = Actor.Named("Alice");
/// actor.Can(BrowseTheWeb.Using(page));   // NScreenplay.Playwright
/// await actor.AttemptsTo(Login.WithCredentials("alice@example.com", "secret"));
/// var title = await actor.AsksFor(Text.Of(Dashboard.Title));
/// await actor.Should(See.That(Dashboard.IsDisplayed()));
/// </code>
/// </example>
/// </remarks>
public sealed class Actor : IAsyncDisposable
{
    private readonly Dictionary<Type, IAbility> _abilities = [];
    private bool _disposed;

    /// <summary>The name of this actor, used in logs and error messages.</summary>
    public string Name { get; }

    private Actor(string name) => Name = name;

    /// <summary>Creates a new actor with the given name.</summary>
    public static Actor Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Actor(name);
    }

    /// <summary>
    /// Grants the actor an ability. If an ability of the same type already exists it is replaced.
    /// </summary>
    /// <returns>This actor, enabling a fluent chain: <c>actor.Can(A).Can(B)</c>.</returns>
    public Actor Can(IAbility ability)
    {
        ArgumentNullException.ThrowIfNull(ability);
        _abilities[ability.GetType()] = ability;
        return this;
    }

    /// <summary>
    /// Returns the ability of type <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="MissingAbilityException">
    /// When the actor does not have the requested ability.
    /// </exception>
    public T GetAbility<T>() where T : IAbility
    {
        if (_abilities.TryGetValue(typeof(T), out var ability))
            return (T)ability;

        throw new MissingAbilityException(Name, typeof(T));
    }

    /// <summary>Returns <see langword="true"/> if the actor has the ability of type <typeparamref name="T"/>.</summary>
    public bool HasAbility<T>() where T : IAbility =>
        _abilities.ContainsKey(typeof(T));

    /// <summary>
    /// Attempts to perform a single <see cref="IPerformable"/> (task or interaction).
    /// </summary>
    public Task AttemptsTo(IPerformable performable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(performable);
        cancellationToken.ThrowIfCancellationRequested();
        return performable.PerformAs(this, cancellationToken);
    }

    /// <summary>
    /// Attempts to perform multiple <see cref="IPerformable"/> items in sequence.
    /// </summary>
    public async Task AttemptsTo(IEnumerable<IPerformable> performables, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(performables);
        foreach (var performable in performables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await performable.PerformAs(this, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asks the actor to answer a question and returns the result.
    /// </summary>
    public Task<TAnswer> AsksFor<TAnswer>(IQuestion<TAnswer> question, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);
        return question.AnsweredBy(this, cancellationToken);
    }

    /// <summary>
    /// Evaluates a consequence (verification). Throws if the expectation is not met.
    /// </summary>
    public Task Should(IConsequence consequence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consequence);
        return consequence.EvaluateAs(this, cancellationToken);
    }

    /// <summary>
    /// Evaluates multiple consequences in sequence. All must pass.
    /// </summary>
    public async Task Should(IEnumerable<IConsequence> consequences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consequences);
        foreach (var consequence in consequences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await consequence.EvaluateAs(this, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes the actor and any abilities that implement <see cref="IAsyncDisposable"/>.
    /// Call this at the end of each test scenario to release browser pages, HTTP connections, etc.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        Exception? firstError = null;
        foreach (var ability in _abilities.Values.OfType<IAsyncDisposable>())
        {
            try
            {
                await ability.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (firstError is null)
            {
                // keep disposing remaining abilities; rethrow the first failure at the end
                firstError = ex;
            }
            catch
            {
                // additional failures after the first are swallowed to let cleanup finish
            }
        }

        _abilities.Clear();

        if (firstError is not null)
            throw firstError;
    }

    /// <inheritdoc/>
    public override string ToString() => $"Actor({Name})";
}
