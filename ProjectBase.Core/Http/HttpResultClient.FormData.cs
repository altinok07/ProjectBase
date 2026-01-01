using ProjectBase.Core.Results;

namespace ProjectBase.Core.Http;

public sealed partial class HttpResultClient
{
    public Task<Result<TResponse>> PostFormDataAsync<TResponse>(
        string clientName,
        string url,
        IDictionary<string, string?>? fields = null,
        IEnumerable<FormFilePart>? files = null,
        IDictionary<string, string?>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent();

        if (fields is not null)
        {
            foreach (var (key, value) in fields)
            {
                if (string.IsNullOrWhiteSpace(key) || value is null) continue;
                form.Add(new StringContent(value), key);
            }
        }

        if (files is not null)
        {
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.Name)) continue;
                if (file.Content is null) continue;
                if (string.IsNullOrWhiteSpace(file.FileName)) continue;

                var stream = file.LeaveOpen ? new NonDisposingStream(file.Content) : file.Content;
                var fileContent = new StreamContent(stream);

                if (!string.IsNullOrWhiteSpace(file.ContentType))
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                form.Add(fileContent, file.Name, file.FileName);
            }
        }

        return SendAsyncCore<TResponse>(clientName, HttpMethod.Post, url, content: form, headers, cancellationToken);
    }
}


