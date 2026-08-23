using NScreenplay.Core;

namespace NScreenplay.Core.Tests.TestDoubles;

/// <summary>A simple ability used in tests. Carries no real functionality.</summary>
internal sealed class FakeAbility : IAbility
{
    public bool WasUsed { get; private set; }

    public void MarkUsed() => WasUsed = true;
}

/// <summary>A second, distinct ability type to test multi-ability scenarios.</summary>
internal sealed class AnotherAbility : IAbility { }

/// <summary>An ability that implements IAsyncDisposable for lifecycle tests.</summary>
internal sealed class DisposableAbility : IAbility, IAsyncDisposable
{
    public bool WasDisposed { get; private set; }
    public int DisposeCount { get; private set; }

    public ValueTask DisposeAsync()
    {
        WasDisposed = true;
        DisposeCount++;
        return ValueTask.CompletedTask;
    }
}

/// <summary>A second distinct disposable ability for multi-disposal tests.</summary>
internal sealed class AnotherDisposableAbility : IAbility, IAsyncDisposable
{
    public bool WasDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        WasDisposed = true;
        return ValueTask.CompletedTask;
    }
}

/// <summary>An ability whose disposal throws, for failure-path tests.</summary>
internal sealed class FailingDisposableAbility : IAbility, IAsyncDisposable
{
    public bool DisposeAttempted { get; private set; }

    public ValueTask DisposeAsync()
    {
        DisposeAttempted = true;
        throw new InvalidOperationException("Disposal failed intentionally.");
    }
}

/// <summary>A performable that records whether and how it was executed.</summary>
internal sealed class FakePerformable : IPerformable
{
    public int ExecutionCount { get; private set; }
    public Actor? LastActor { get; private set; }
    public CancellationToken LastToken { get; private set; }

    private readonly Exception? _throws;

    public FakePerformable(Exception? throws = null) => _throws = throws;

    public Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_throws is not null) throw _throws;
        ExecutionCount++;
        LastActor = actor;
        LastToken = cancellationToken;
        return Task.CompletedTask;
    }
}

/// <summary>A performable backed by a callback, for flexible test scenarios.</summary>
internal sealed class CallbackPerformable : IPerformable
{
    private readonly Func<Task> _callback;

    public CallbackPerformable(Func<Task> callback) => _callback = callback;

    public Task PerformAs(Actor actor, CancellationToken cancellationToken = default) =>
        _callback();
}

/// <summary>A task that composes two inner performables to validate task composition.</summary>
internal sealed class CompositeTask : ITask
{
    private readonly IPerformable _first;
    private readonly IPerformable _second;

    public CompositeTask(IPerformable first, IPerformable second)
    {
        _first = first;
        _second = second;
    }

    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        await _first.PerformAs(actor, cancellationToken).ConfigureAwait(false);
        await _second.PerformAs(actor, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>A question that returns a preconfigured answer.</summary>
internal sealed class FakeQuestion<T> : IQuestion<T>
{
    private readonly T _answer;
    public Actor? LastActor { get; private set; }

    public FakeQuestion(T answer) => _answer = answer;

    public Task<T> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastActor = actor;
        return Task.FromResult(_answer);
    }
}

/// <summary>A question that captures the CancellationToken it received.</summary>
internal sealed class TokenCapturingQuestion : IQuestion<bool>
{
    public CancellationToken ReceivedToken { get; private set; }

    public Task<bool> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return Task.FromResult(true);
    }
}

/// <summary>A question that throws a specified exception.</summary>
internal sealed class ThrowingQuestion : IQuestion<bool>
{
    private readonly Exception _exception;

    public ThrowingQuestion(Exception exception) => _exception = exception;

    public Task<bool> AnsweredBy(Actor actor, CancellationToken cancellationToken = default) =>
        throw _exception;
}

/// <summary>A consequence that passes or fails based on construction.</summary>
internal sealed class FakeConsequence : IConsequence
{
    private readonly bool _shouldPass;
    public bool WasEvaluated { get; private set; }
    public Actor? LastActor { get; private set; }

    public FakeConsequence(bool shouldPass = true) => _shouldPass = shouldPass;

    public Task EvaluateAs(Actor actor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WasEvaluated = true;
        LastActor = actor;
        if (!_shouldPass)
            throw new InvalidOperationException("Fake consequence failed intentionally.");
        return Task.CompletedTask;
    }
}

/// <summary>A consequence that throws a specified exception.</summary>
internal sealed class ThrowingConsequence : IConsequence
{
    private readonly Exception _exception;

    public ThrowingConsequence(Exception exception) => _exception = exception;

    public Task EvaluateAs(Actor actor, CancellationToken cancellationToken = default) =>
        throw _exception;
}
