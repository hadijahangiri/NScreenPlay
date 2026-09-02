using NScreenplay.Core;
using WalletApi.Api;
using WalletApi.Questions;

namespace WalletApi.Tasks;

public sealed class CreateWallet : ITask
{
    private readonly CreateWalletRequest _request;

    private CreateWallet(CreateWalletRequest request) => _request = request;

    public static CreateWallet For(string customerId, string currency, decimal initialBalance) =>
        new(new CreateWalletRequest(customerId, currency, initialBalance));

    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var wallet = await actor.GetAbility<WalletApiAbility>()
            .PostAsync<CreateWalletRequest, WalletResponse>("wallets", _request, cancellationToken)
            .ConfigureAwait(false);

        actor.Can(new CurrentWalletAbility(wallet));
    }
}

internal sealed record CreateWalletRequest(string CustomerId, string Currency, decimal InitialBalance);

internal sealed class CurrentWalletAbility : IAbility
{
    public WalletResponse Wallet { get; }

    public CurrentWalletAbility(WalletResponse wallet) => Wallet = wallet;
}