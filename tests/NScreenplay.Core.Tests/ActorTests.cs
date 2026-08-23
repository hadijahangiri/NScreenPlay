using NScreenplay.Core;
using NScreenplay.Core.Tests.TestDoubles;

namespace NScreenplay.Core.Tests;

public class ActorTests
{
    // ── Creation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Named_SetsActorName()
    {
        var actor = Actor.Named("Alice");
        Assert.Equal("Alice", actor.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Named_ThrowsForBlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => Actor.Named(name));
    }

    [Fact]
    public void Named_ThrowsForNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Actor.Named(null!));
    }

    [Fact]
    public void ToString_IncludesActorName()
    {
        var actor = Actor.Named("Bob");
        Assert.Contains("Bob", actor.ToString());
    }

    // ── Ability registration ──────────────────────────────────────────────────

    [Fact]
    public void Can_GrantsAbility()
    {
        var actor = Actor.Named("Alice");
        actor.Can(new FakeAbility());
        Assert.True(actor.HasAbility<FakeAbility>());
    }

    [Fact]
    public void Can_ReturnsActorForFluentChaining()
    {
        var actor = Actor.Named("Alice");
        var returned = actor.Can(new FakeAbility());
        Assert.Same(actor, returned);
    }

    [Fact]
    public void Can_ReplacesExistingAbilityOfSameType()
    {
        var actor = Actor.Named("Alice");
        var first = new FakeAbility();
        var second = new FakeAbility();
        actor.Can(first);
        actor.Can(second);
        Assert.Same(second, actor.GetAbility<FakeAbility>());
    }

    [Fact]
    public void Can_ThrowsForNullAbility()
    {
        var actor = Actor.Named("Alice");
        Assert.Throws<ArgumentNullException>(() => actor.Can(null!));
    }

    // ── Ability retrieval ─────────────────────────────────────────────────────

    [Fact]
    public void GetAbility_ReturnsRegisteredAbility()
    {
        var actor = Actor.Named("Alice");
        var ability = new FakeAbility();
        actor.Can(ability);
        Assert.Same(ability, actor.GetAbility<FakeAbility>());
    }

    [Fact]
    public void GetAbility_ThrowsMissingAbilityExceptionWhenNotRegistered()
    {
        var actor = Actor.Named("Alice");
        var ex = Assert.Throws<MissingAbilityException>(() => actor.GetAbility<FakeAbility>());
        Assert.Equal("Alice", ex.ActorName);
        Assert.Equal(typeof(FakeAbility), ex.AbilityType);
    }

    [Fact]
    public void GetAbility_ExceptionMessageContainsActorName()
    {
        var actor = Actor.Named("Alice");
        var ex = Assert.Throws<MissingAbilityException>(() => actor.GetAbility<FakeAbility>());
        Assert.Contains("Alice", ex.Message);
    }

    [Fact]
    public void HasAbility_ReturnsFalseWhenNotRegistered()
    {
        var actor = Actor.Named("Alice");
        Assert.False(actor.HasAbility<FakeAbility>());
    }

    [Fact]
    public void HasAbility_ReturnsTrueAfterCan()
    {
        var actor = Actor.Named("Alice");
        actor.Can(new FakeAbility());
        Assert.True(actor.HasAbility<FakeAbility>());
    }

    [Fact]
    public void MultipleAbilityTypes_AreStoredIndependently()
    {
        var actor = Actor.Named("Alice");
        actor.Can(new FakeAbility()).Can(new AnotherAbility());
        Assert.True(actor.HasAbility<FakeAbility>());
        Assert.True(actor.HasAbility<AnotherAbility>());
    }

    // ── Task / Interaction execution ──────────────────────────────────────────

    [Fact]
    public async Task AttemptsTo_ExecutesPerformable()
    {
        var actor = Actor.Named("Alice");
        var performable = new FakePerformable();
        await actor.AttemptsTo(performable);
        Assert.Equal(1, performable.ExecutionCount);
    }

    [Fact]
    public async Task AttemptsTo_PassesActorToPerformable()
    {
        var actor = Actor.Named("Alice");
        var performable = new FakePerformable();
        await actor.AttemptsTo(performable);
        Assert.Same(actor, performable.LastActor);
    }

    [Fact]
    public async Task AttemptsTo_ThrowsWhenPerformableThrows()
    {
        var actor = Actor.Named("Alice");
        var performable = new FakePerformable(throws: new InvalidOperationException("boom"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.AttemptsTo(performable));
    }

    [Fact]
    public async Task AttemptsTo_ThrowsForNullPerformable()
    {
        var actor = Actor.Named("Alice");
        await Assert.ThrowsAsync<ArgumentNullException>(() => actor.AttemptsTo((IPerformable)null!));
    }

    [Fact]
    public async Task AttemptsTo_ExecutesMultiplePerformablesInSequence()
    {
        var actor = Actor.Named("Alice");
        var first = new FakePerformable();
        var second = new FakePerformable();
        await actor.AttemptsTo([first, second]);
        Assert.Equal(1, first.ExecutionCount);
        Assert.Equal(1, second.ExecutionCount);
    }

    // ── Question execution ────────────────────────────────────────────────────

    [Fact]
    public async Task AsksFor_ReturnsAnswer()
    {
        var actor = Actor.Named("Alice");
        var question = new FakeQuestion<string>("hello");
        var answer = await actor.AsksFor(question);
        Assert.Equal("hello", answer);
    }

    [Fact]
    public async Task AsksFor_PassesActorToQuestion()
    {
        var actor = Actor.Named("Alice");
        var question = new FakeQuestion<int>(42);
        await actor.AsksFor(question);
        Assert.Same(actor, question.LastActor);
    }

    [Fact]
    public async Task AsksFor_ThrowsForNullQuestion()
    {
        var actor = Actor.Named("Alice");
        await Assert.ThrowsAsync<ArgumentNullException>(() => actor.AsksFor<string>(null!));
    }

    // ── Consequence execution ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_EvaluatesConsequence()
    {
        var actor = Actor.Named("Alice");
        var consequence = new FakeConsequence(shouldPass: true);
        await actor.Should(consequence);
        Assert.True(consequence.WasEvaluated);
    }

    [Fact]
    public async Task Should_ThrowsWhenConsequenceFails()
    {
        var actor = Actor.Named("Alice");
        var consequence = new FakeConsequence(shouldPass: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Should(consequence));
    }

    [Fact]
    public async Task Should_PassesActorToConsequence()
    {
        var actor = Actor.Named("Alice");
        var consequence = new FakeConsequence(shouldPass: true);
        await actor.Should(consequence);
        Assert.Same(actor, consequence.LastActor);
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AttemptsTo_RespectsAlreadyCancelledToken()
    {
        var actor = Actor.Named("Alice");
        var performable = new FakePerformable();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => actor.AttemptsTo(performable, cts.Token));
        Assert.Equal(0, performable.ExecutionCount);
    }

    [Fact]
    public async Task AsksFor_RespectsAlreadyCancelledToken()
    {
        var actor = Actor.Named("Alice");
        var question = new FakeQuestion<string>("unused");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => actor.AsksFor(question, cts.Token));
    }

    [Fact]
    public async Task Should_RespectsAlreadyCancelledToken()
    {
        var actor = Actor.Named("Alice");
        var consequence = new FakeConsequence(shouldPass: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => actor.Should(consequence, cts.Token));
    }

    // ── Isolation ────────────────────────────────────────────────────────────

    [Fact]
    public void TwoActors_DoNotShareAbilities()
    {
        var alice = Actor.Named("Alice");
        var bob = Actor.Named("Bob");
        alice.Can(new FakeAbility());
        Assert.True(alice.HasAbility<FakeAbility>());
        Assert.False(bob.HasAbility<FakeAbility>());
    }

    [Fact]
    public void Actor_HasNoGlobalState_CreatingSecondActorDoesNotAffectFirst()
    {
        var alice = Actor.Named("Alice");
        alice.Can(new FakeAbility());
        _ = Actor.Named("Bob");
        // Alice must still have her ability after Bob was created
        Assert.True(alice.HasAbility<FakeAbility>());
    }

    // ── Task composition ──────────────────────────────────────────────────────

    [Fact]
    public async Task CompositeTask_ExecutesBothInnerPerformables()
    {
        var actor = Actor.Named("Alice");
        var step1 = new FakePerformable();
        var step2 = new FakePerformable();
        var task = new CompositeTask(step1, step2);
        await actor.AttemptsTo(task);
        Assert.Equal(1, step1.ExecutionCount);
        Assert.Equal(1, step2.ExecutionCount);
    }

    [Fact]
    public async Task CompositeTask_ExceptionInFirstStepPreventsSecond()
    {
        var actor = Actor.Named("Alice");
        var step1 = new FakePerformable(throws: new InvalidOperationException("first fails"));
        var step2 = new FakePerformable();
        var task = new CompositeTask(step1, step2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.AttemptsTo(task));
        Assert.Equal(0, step2.ExecutionCount);
    }
}
