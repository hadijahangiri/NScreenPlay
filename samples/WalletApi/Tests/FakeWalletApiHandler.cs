using System.Text;
using System.Text.Json;

namespace WalletApi.Tests;

internal sealed class FakeWalletApiHandler : HttpMessageHandler
{
    private const string Token = "demo-token";
    private readonly Dictionary<string, WalletDto> _wallets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wallet-123"] = new WalletDto("wallet-123", "cust-42", "USD", 25.50m)
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is null)
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));

        if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.StartsWith("/wallets/", StringComparison.OrdinalIgnoreCase))
        {
            var walletId = request.RequestUri.AbsolutePath["/wallets/".Length..];
            if (_wallets.TryGetValue(walletId, out var wallet))
                return JsonResponse(System.Net.HttpStatusCode.OK, wallet);

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }

        if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/wallets", StringComparison.OrdinalIgnoreCase))
        {
            var body = JsonSerializer.Deserialize<CreateWalletRequest>(request.Content!.ReadAsStringAsync(cancellationToken).Result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var wallet = new WalletDto("wallet-456", body!.CustomerId, body.Currency, body.InitialBalance);
            _wallets[wallet.Id] = wallet;
            return JsonResponse(System.Net.HttpStatusCode.Created, wallet);
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private static Task<HttpResponseMessage> JsonResponse<T>(System.Net.HttpStatusCode code, T payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Task.FromResult(new HttpResponseMessage(code)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    internal sealed record WalletDto(string Id, string CustomerId, string Currency, decimal Balance);
    internal sealed record CreateWalletRequest(string CustomerId, string Currency, decimal InitialBalance);
}