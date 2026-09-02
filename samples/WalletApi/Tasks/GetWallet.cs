using NScreenplay.Core;
using WalletApi.Api;
using WalletApi.Questions;

namespace WalletApi.Tasks;

public sealed class GetWallet : ITask
{
    private readonly string _walletId;

    private GetWallet(string walletId) => _walletId = walletId;

    public static GetWallet For(string walletId) => new(walletId);

    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var wallet = await actor.GetAbility<WalletApiAbility>()
            .GetAsync<WalletResponse>($"wallets/{_walletId}", cancellationToken)
            .ConfigureAwait(false);

        actor.Can(new CurrentWalletAbility(wallet));
    }
}