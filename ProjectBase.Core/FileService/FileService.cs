using FluentFTP;
using Microsoft.Extensions.Options;
using ProjectBase.Core.FileService.Interfaces;
using ProjectBase.Core.FileService.Models;

namespace ProjectBase.Core.FileService
{
    public class FileService(IOptions<FtpSettings> options) : IFileService
    {
        private readonly FtpSettings _settings = options.Value;

        public async Task RemoveAsync(string pathOrUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return;
            if (!pathOrUrl.Contains(GetHost(), StringComparison.OrdinalIgnoreCase))
                return;

            var remotePath = ToRemotePath(pathOrUrl);

            await using var client = CreateClient();
            await client.Connect(cancellationToken);
            try
            {
                if (await client.FileExists(remotePath, cancellationToken))
                    await client.DeleteFile(remotePath, cancellationToken);
            }
            finally
            {
                await client.Disconnect(cancellationToken);
            }
        }

        public async Task<FileUploadResult> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.File == null) throw new ArgumentException("File is required.", nameof(request));

            var extension = Path.GetExtension(request.File.FileName);
            string fileName = $"{Guid.NewGuid():N}{extension}";
            var relativePath = BuildRelativePath(request.Folder, fileName);

            await using var client = CreateClient();
            await client.Connect(cancellationToken);
            try
            {
                await EnsureDirectoriesExistAsync(client, request.Folder, cancellationToken);

                await using var fileStream = request.File.OpenReadStream();
                await client.UploadStream(fileStream, relativePath, token: cancellationToken);
            }
            finally
            {
                await client.Disconnect(cancellationToken);
            }

            return new FileUploadResult
            {
                Extension = extension,
                SizeBytes = request.File.Length,
                Path = BuildPublicUrl(relativePath)
            };
        }

        public async Task<FileUploadResult> UploadBase64Async(Base64UploadRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Base64Content))
                throw new ArgumentException("Base64 content is required.", nameof(request));

            var base64Content = StripDataUriPrefix(request.Base64Content);
            var extension = NormalizeExtension(request.Extension);
            string fileName = $"{Guid.NewGuid():N}{extension}";
            var relativePath = BuildRelativePath(request.Folder, fileName);

            byte[] fileContents = Convert.FromBase64String(base64Content);

            await using var client = CreateClient();
            await client.Connect(cancellationToken);
            try
            {
                await EnsureDirectoriesExistAsync(client, request.Folder, cancellationToken);
                await client.UploadBytes(fileContents, relativePath, token: cancellationToken);
            }
            finally
            {
                await client.Disconnect(cancellationToken);
            }

            return new FileUploadResult
            {
                Extension = extension,
                SizeBytes = fileContents.LongLength,
                Path = BuildPublicUrl(relativePath)
            };
        }

        private AsyncFtpClient CreateClient()
        {
            var (host, port) = GetHostAndPort();
            return new AsyncFtpClient(host, _settings.Username, _settings.Password, port);
        }

        private static string StripDataUriPrefix(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return content;
            if (!content.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return content;

            var commaIndex = content.IndexOf(',');
            return commaIndex >= 0 ? content[(commaIndex + 1)..] : content;
        }

        private static string NormalizeExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
            return extension.StartsWith('.') ? extension : "." + extension;
        }

        private static string BuildRelativePath(string folder, string fileName)
        {
            var subFolder = (folder ?? string.Empty).Trim('/');
            return string.IsNullOrEmpty(subFolder) ? fileName : $"{subFolder}/{fileName}";
        }

        private static async Task EnsureDirectoriesExistAsync(AsyncFtpClient client, string folder, CancellationToken cancellationToken)
        {
            var folderPath = (folder ?? string.Empty).Trim('/');
            if (string.IsNullOrEmpty(folderPath)) return;

            var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = string.Empty;

            foreach (var segment in segments)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                if (!await client.DirectoryExists(currentPath, cancellationToken))
                    await client.CreateDirectory(currentPath, cancellationToken);
            }
        }

        private string ToRemotePath(string pathOrUrl)
        {
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute))
            {
                if (absolute.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
                    return absolute.AbsolutePath.TrimStart('/');
                return absolute.AbsolutePath.TrimStart('/');
            }
            return pathOrUrl.TrimStart('/');
        }

        private string BuildPublicUrl(string relativePath)
        {
            var host = GetHost();
            return $"https://{host.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }

        private string GetHost()
        {
            var (host, _) = GetHostAndPort();
            return host;
        }

        private (string Host, int Port) GetHostAndPort()
        {
            if (string.IsNullOrWhiteSpace(_settings.FtpAddress))
                throw new InvalidOperationException($"{nameof(FtpSettings)}:{nameof(FtpSettings.FtpAddress)} gerekli.");

            var addr = _settings.FtpAddress.Trim();

            if (Uri.TryCreate(addr, UriKind.Absolute, out var uri))
            {
                if (!uri.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"FtpAddress FTP adresi olmalı (ftp://...).");
                return (uri.Host, uri.Port > 0 ? uri.Port : 21);
            }

            if (Uri.TryCreate("ftp://" + addr.TrimStart('/'), UriKind.Absolute, out uri))
                return (uri.Host, uri.Port > 0 ? uri.Port : 21);

            var hostPart = addr.Split('/')[0];
            var portMatch = hostPart.Contains(':');
            if (portMatch)
            {
                var parts = hostPart.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var port))
                    return (parts[0], port);
            }
            return (hostPart, 21);
        }
    }
}
