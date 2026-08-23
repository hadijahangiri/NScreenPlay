using NScreenplay.Core;
using NScreenplay.Core.Tests.TestDoubles;

namespace NScreenplay.Core.Tests;

/// <summary>
/// Tests for cancellation behavior across the mid-sequence and async paths
/// not covered by ActorTests (which focused on pre-cancelled tokens).
/// </summary>
public class CancellationTests
{
    // ── Mid-sequence cancellation in AttemptsTo(IEnumerable) ─────────────────

    [Fact]
    public async Task AttemptsTo_Sequence_CancellationMidway_StopsExecution()
    {
        var actor = Actor.Named("Alice");
        using var cts = new CancellationTokenSource();

        int executionCount = 0;
        // First performable cancels the token; second must never run
        var first = new CallbackPerformable(() =>
        {
            executionCount++;
            cts.Cancel();
            return Task.CompletedTask;
        });
        var second = new CallbackPerformable(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => actor.AttemptsTo([first, second], cts.Token));

        Assert.Equal(1, executionCount);
    }

    // ── Should(IEnumerable<IConsequence>) ────────────────────────────────────

    [Fact]
    public async Task Should_Sequence_EvaluatesAllPassingConsequences()
    {
        var actor = Actor.Named("Alice");
        var c1 = new FakeConsequence(shouldPass: true);
        var c2 = new FakeConsequence(shouldPass: true);
        await actor.Should([c1, c2]);
        Assert.True(c1.WasEvaluated);
        Assert.True(c2.WasEvaluated);
    }

    [Fact]
    public async Task Should_Sequence_StopsOnFirstFailure()
    {
        var actor = Actor.Named("Alice");
        var c1 = new FakeConsequence(shouldPass: false);
        var c2 = new FakeConsequence(shouldPass: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Should([c1, c2]));
        Assert.False(c2.WasEvaluated);
    }

    [Fact]
    public async Task Should_Sequence_RespectsCancellationMidway()
    {
        var actor = Actor.Named("Alice");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var c1 = new FakeConsequence(shouldPass: true);
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => actor.Should([c1], cts.Token));
        Assert.False(c1.WasEvaluated);
    }

    // ── Token propagation ────────────────────────────────────────────────────

    [Fact]
    public async Task AttemptsTo_Single_PropagatesTokenToPerformable()
    {
        var actor = Actor.Named("Alice");
        using var cts = new CancellationTokenSource();
        var performable = new FakePerformable();
        await actor.AttemptsTo(performable, cts.Token);
        Assert.Equal(cts.Token, performable.LastToken);
    }

    [Fact]
    public async Task AsksFor_PropagatesTokenToQuestion()
    {
        var actor = Actor.Named("Alice");
        using var cts = new CancellationTokenSource();
        var question = new TokenCapturingQuestion();
        await actor.AsksFor(question, cts.Token);
        Assert.Equal(cts.Token, question.ReceivedToken);
    }

    // ── Exception propagation (no wrapping) ──────────────────────────────────

    [Fact]
    public async Task AsksFor_PropagatesExceptionUnwrapped()
    {
        var actor = Actor.Named("Alice");
        var question = new ThrowingQuestion(new InvalidOperationException("question failed"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => actor.AsksFor(question));
        Assert.Equal("question failed", ex.Message);
    }

    [Fact]
    public async Task Should_PropagatesExceptionUnwrapped()
    {
        var actor = Actor.Named("Alice");
        var consequence = new ThrowingConsequence(new ArgumentException("bad state"));
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => actor.Should(consequence));
        Assert.Equal("bad state", ex.Message);
    }
}
