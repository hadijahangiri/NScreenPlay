using NScreenplay.Core;
using WalletApi.Api;

namespace WalletApi.Questions;

public sealed class WalletBalance : IQuestion<decimal>
{
    private readonly string _walletId;

    private WalletBalance(string walletId) => _walletId = walletId;

    public static WalletBalance For(string walletId) => new(walletId);

    public async Task<decimal> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var wallet = await actor.AsksFor(WalletById.For(_walletId), cancellationToken).ConfigureAwait(false);
        return wallet.Balance;
    }
}

internal sealed class WalletById : IQuestion<WalletResponse>
{
    private readonly string _walletId;

    private WalletById(string walletId) => _walletId = walletId;

    public static WalletById For(string walletId) => new(walletId);

    public Task<WalletResponse> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var ability = actor.GetAbility<WalletApiAbility>();
        return ability.GetAsync<WalletResponse>($"wallets/{_walletId}", cancellationToken);
    }
}