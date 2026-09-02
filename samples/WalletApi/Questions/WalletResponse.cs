using System.Text.Json.Serialization;

namespace WalletApi.Questions;

public sealed class WalletResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; init; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("balance")]
    public decimal Balance { get; init; }
}