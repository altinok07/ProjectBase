using ProjectBase.Core.Results;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjectBase.Core.Http;

public sealed partial class HttpResultClient
{
    public async Task<Result<TResponse>> SendAsync<TResponse>(
        string clientName,
        HttpMethod method,
        string url,
        object? body = null,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default)
    {
        HttpContent? content = null;
        if (body is not null)
            content = JsonContent.Create(body, options: JsonOptions);

        return await SendAsyncCore<TResponse>(clientName, method, url, content, headers, cancellationToken);
    }

    private async Task<Result<TResponse>> SendAsyncCore<TResponse>(
        string clientName,
        HttpMethod method,
        string url,
        HttpContent? content,
        IDictionary<string, string?>? headers,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(clientName);

            var normalizedUrl = NormalizeUrl(client.BaseAddress, url);
            var requestUri = Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var absoluteUri)
                ? absoluteUri
                : new Uri(normalizedUrl, UriKind.Relative);

            using var request = new HttpRequestMessage(method, requestUri);

            if (content is not null)
                request.Content = content;

            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                {
                    if (string.IsNullOrWhiteSpace(key) || value is null) continue;

                    // First try request headers, then content headers.
                    if (!request.Headers.TryAddWithoutValidation(key, value))
                        request.Content?.Headers.TryAddWithoutValidation(key, value);
                }
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var resultType = MapResultType(response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return Result<TResponse>.Success(ResultType.NoContent, default!, "NoContent");

                if (typeof(TResponse) == typeof(string))
                {
                    var s = await response.Content.ReadAsStringAsync(cancellationToken);
                    return Result<TResponse>.Success(resultType, (TResponse)(object)s, "Success");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(json))
                    return Result<TResponse>.Success(resultType, default!, "Success");

                try
                {
                    var direct = JsonSerializer.Deserialize<TResponse>(json, JsonOptions);
                    if (direct is not null)
                        return Result<TResponse>.Success(resultType, direct, "Success");

                    return Result<TResponse>.Fail(ResultType.InternalServerError, "ResponseDeserializationFailed");
                }
                catch (Exception ex)
                {
                    return DeserializeError<TResponse>(ex);
                }
            }

            var errorPayload = await TryReadJsonAsync<ResultPayload>(response, cancellationToken);
            var message = errorPayload?.Message ?? $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
            var errors = errorPayload?.Errors;

            if (errors is { Count: > 0 })
                return Result<TResponse>.Fail(resultType, message, errors);

            return Result<TResponse>.Fail(resultType, message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return ExceptionError<TResponse>(ex, "RequestTimeout");
        }
        catch (HttpRequestException ex)
        {
            return ExceptionError<TResponse>(ex, "HttpRequestFailed");
        }
        catch (Exception ex)
        {
            return ExceptionError<TResponse>(ex, "UnexpectedHttpClientError");
        }
    }
}