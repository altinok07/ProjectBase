using System.Text.Json;
using ProjectBase.Core.Results;

namespace ProjectBase.Core.Http;

public sealed partial class HttpResultClient : IHttpResultClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpResultClient(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<Result<TResponse>> GetAsync<TResponse>(
        string clientName,
        string url,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(clientName, HttpMethod.Get, url, body: null, headers, cancellationToken);

    public Task<Result<TResponse>> PostJsonAsync<TResponse>(
        string clientName,
        string url,
        object? body,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(clientName, HttpMethod.Post, url, body, headers, cancellationToken);
}