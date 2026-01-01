using ProjectBase.Core.Results;
using System.Net;
using System.Net.Http.Json;

namespace ProjectBase.Core.Http;

public sealed partial class HttpResultClient
{
    private static async Task<T?> TryReadJsonAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            if (response.Content.Headers.ContentLength == 0)
                return default;

            // If content-type is missing/odd, ReadFromJsonAsync may throw; that's fine, we fall back.
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
        catch
        {
            return default;
        }
    }

    private static Result<T> DeserializeError<T>(Exception ex)
    {
        var baseException = ex.GetBaseException();
        var errors = new List<ErrorResult>
        {
            new ErrorResult(
                propertyName: null,
                errorMessage: baseException.Message,
                errorCode: baseException.GetType().Name)
        };

        return Result<T>.Fail(ResultType.InternalServerError, "ResponseDeserializationFailed", errors);
    }

    private static string NormalizeUrl(Uri? baseAddress, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // Absolute URL: don't touch it.
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
            return url;

        // If BaseAddress includes a path (e.g. https://host/api/v1/...), then a leading '/'
        // would reset to host root (https://host/City) and drop that path. Most callers expect append.
        if (baseAddress is not null &&
            !string.Equals(baseAddress.AbsolutePath, "/", StringComparison.Ordinal) &&
            url.StartsWith("/", StringComparison.Ordinal))
        {
            return url.TrimStart('/');
        }

        return url;
    }

    private static ResultType MapResultType(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code switch
        {
            200 => ResultType.Success,
            201 => ResultType.Created,
            204 => ResultType.NoContent,
            400 => ResultType.BadRequest,
            401 => ResultType.Unauthorized,
            403 => ResultType.Forbidden,
            404 => ResultType.NotFound,
            409 => ResultType.Conflict,
            422 => ResultType.UnprocessableEntity,
            429 => ResultType.TooManyRequests,
            500 => ResultType.InternalServerError,
            _ => ResultType.InternalServerError
        };
    }

    private static Result<T> ExceptionError<T>(Exception ex, string message)
    {
        var baseException = ex.GetBaseException();
        var errors = new List<ErrorResult>
        {
            new ErrorResult(
                propertyName: null,
                errorMessage: baseException.Message,
                errorCode: baseException.GetType().Name)
        };

        return Result<T>.Fail(ResultType.InternalServerError, message, errors);
    }

    private sealed class ResultPayload
    {
        public string? Message { get; init; }
        public List<ErrorResult>? Errors { get; init; }
    }

    private sealed class NonDisposingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        protected override void Dispose(bool disposing) { /* intentionally does not dispose inner */ }
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.WriteAsync(buffer, cancellationToken);
    }
}


