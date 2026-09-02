using NScreenplay.Core;
using WalletApi.Api;
using WalletApi.Questions;
using WalletApi.Tasks;

namespace WalletApi.Tests;

public sealed class WalletTests : IAsyncLifetime
{
    private readonly Actor _actor = Actor.Named("Wallet Tester");
    private readonly FakeWalletApiHandler _handler = new();

    public Task InitializeAsync()
    {
        _actor.Can(WalletApiAbility.For(new Uri("https://wallet.example.test/"), _handler, bearerToken: "demo-token"));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _actor.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task GetWallet_ReturnsWalletResponse()
    {
        await _actor.AttemptsTo(GetWallet.For("wallet-123"));

        var wallet = await _actor.AsksFor(CurrentWallet.For());

        Assert.Equal("wallet-123", wallet.Id);
        Assert.Equal("cust-42", wallet.CustomerId);
        Assert.Equal("USD", wallet.Currency);
    }

    [Fact]
    public async Task CreateWallet_ReturnsWalletResponse()
    {
        await _actor.AttemptsTo(CreateWallet.For("cust-99", "EUR", 125.50m));

        var wallet = await _actor.AsksFor(CurrentWallet.For());

        Assert.Equal("wallet-456", wallet.Id);
        Assert.Equal("cust-99", wallet.CustomerId);
        Assert.Equal("EUR", wallet.Currency);
        Assert.Equal(125.50m, wallet.Balance);
    }

    [Fact]
    public async Task CreateWallet_ReturnsWalletBalance()
    {
        await _actor.AttemptsTo(CreateWallet.For("cust-77", "GBP", 300m));

        var balance = await _actor.AsksFor(WalletBalance.For("wallet-456"));

        Assert.Equal(300m, balance);
    }
}

public sealed class CurrentWallet : IQuestion<WalletResponse>
{
    private CurrentWallet() { }

    public static CurrentWallet For() => new();

    public Task<WalletResponse> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var ability = actor.GetAbility<CurrentWalletAbility>();
        return Task.FromResult(ability.Wallet);
    }
}