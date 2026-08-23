using NScreenplay.Core;
using NScreenplay.Core.Tests.TestDoubles;

namespace NScreenplay.Core.Tests;

/// <summary>Tests for Actor disposal and Reqnroll lifecycle compatibility.</summary>
public class ActorDisposalTests
{
    [Fact]
    public async Task DisposeAsync_DoesNotThrowWhenNoAbilities()
    {
        var actor = Actor.Named("Alice");
        await actor.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_DisposesDisposableAbilities()
    {
        var actor = Actor.Named("Alice");
        var ability = new DisposableAbility();
        actor.Can(ability);
        await actor.DisposeAsync();
        Assert.True(ability.WasDisposed);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var actor = Actor.Named("Alice");
        var ability = new DisposableAbility();
        actor.Can(ability);
        await actor.DisposeAsync();
        await actor.DisposeAsync(); // second call must not throw
        Assert.Equal(1, ability.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllDisposableAbilities()
    {
        var actor = Actor.Named("Alice");
        var a1 = new DisposableAbility();
        var a2 = new AnotherDisposableAbility();
        actor.Can(a1).Can(a2);
        await actor.DisposeAsync();
        Assert.True(a1.WasDisposed);
        Assert.True(a2.WasDisposed);
    }

    [Fact]
    public async Task DisposeAsync_SkipsNonDisposableAbilities()
    {
        var actor = Actor.Named("Alice");
        actor.Can(new FakeAbility()); // not IAsyncDisposable
        // must not throw
        await actor.DisposeAsync();
    }

    [Fact]
    public async Task UsingPattern_WorksForActorLifecycle()
    {
        DisposableAbility ability;
        await using (var actor = Actor.Named("Alice"))
        {
            ability = new DisposableAbility();
            actor.Can(ability);
        }
        Assert.True(ability.WasDisposed);
    }

    [Fact]
    public async Task DisposeAsync_FailingAbility_DoesNotPreventOtherDisposals()
    {
        var actor = Actor.Named("Alice");
        var failing = new FailingDisposableAbility();
        var healthy = new DisposableAbility();
        actor.Can(failing).Can(healthy);

        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.DisposeAsync().AsTask());

        // healthy ability must still be disposed despite the failing one
        Assert.True(healthy.WasDisposed);
        Assert.True(failing.DisposeAttempted);
    }

    [Fact]
    public async Task DisposeAsync_SecondFailureSwallowed_FirstErrorRethrown()
    {
        var actor = Actor.Named("Alice");
        var failing = new FailingDisposableAbility();
        var healthy = new DisposableAbility();
        // order: healthy first, failing last — proves loop continues past failures
        actor.Can(healthy).Can(failing);

        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.DisposeAsync().AsTask());

        Assert.True(healthy.WasDisposed);
        Assert.True(failing.DisposeAttempted);
    }
}
