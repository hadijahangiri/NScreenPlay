using NScreenplay.Core;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WalletApi.Api;

/// <summary>
/// Owns all HTTP concerns for the Wallet API sample.
/// The actor receives this ability and the ability manages HttpClient disposal.
/// </summary>
public sealed class WalletApiAbility : IAbility, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private WalletApiAbility(HttpClient httpClient, Uri baseUri, string? bearerToken)
    {
        _httpClient = httpClient;
        BaseUri = baseUri;
        BearerToken = bearerToken;
    }

    public Uri BaseUri { get; }

    public string? BearerToken { get; private set; }

    public static WalletApiAbility For(Uri baseUri, HttpMessageHandler? handler = null, string? bearerToken = null)
    {
        var httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: true);
        httpClient.BaseAddress = baseUri;
        return new WalletApiAbility(httpClient, baseUri, bearerToken);
    }

    public void Authenticate(string bearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        BearerToken = bearerToken;
    }

    public async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        ApplyAuthorization(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json")
        };
        ApplyAuthorization(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }

    private void ApplyAuthorization(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(BearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
    }

    private async Task<TResponse> ReadJsonAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize {typeof(TResponse).Name}.");
    }
}