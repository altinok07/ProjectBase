using ProjectBase.Core.Results;

namespace ProjectBase.Core.Http;

public interface IHttpResultClient
{
    Task<Result<TResponse>> SendAsync<TResponse>(
        string clientName,
        HttpMethod method,
        string url,
        object? body = null,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default);

    Task<Result<TResponse>> GetAsync<TResponse>(
        string clientName,
        string url,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default);

    Task<Result<TResponse>> PostJsonAsync<TResponse>(
        string clientName,
        string url,
        object? body,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default);

    Task<Result<TResponse>> PostFormDataAsync<TResponse>(
        string clientName,
        string url,
        IDictionary<string, string?>? fields = null,
        IEnumerable<FormFilePart>? files = null,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default);
}


