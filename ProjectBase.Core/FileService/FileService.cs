using Microsoft.Extensions.Options;
using ProjectBase.Core.FileService.Interfaces;
using ProjectBase.Core.FileService.Models;
using System.Net;

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

            var ftpUri = ToFtpUri(pathOrUrl);
            var ftpRequest = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.DeleteFile);

            using var response = (FtpWebResponse)await ftpRequest.GetResponseAsync().WaitAsync(cancellationToken);
            _ = response.StatusDescription;
        }

        public async Task<FileUploadResult> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.File == null) throw new ArgumentException("File is required.", nameof(request));

            var extension = Path.GetExtension(request.File.FileName);
            string fileName = $"{Guid.NewGuid():N}{extension}";
            var relativePath = BuildRelativePath(request.Folder, fileName);

            await EnsureDirectoriesExistAsync(request.Folder, cancellationToken);

            var ftpUri = BuildFtpUri(relativePath);
            var ftpRequest = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.UploadFile);

            await using (var ftpStream = await ftpRequest.GetRequestStreamAsync().WaitAsync(cancellationToken))
            await using (var fileStream = request.File.OpenReadStream())
            {
                await fileStream.CopyToAsync(ftpStream, cancellationToken);
            }

            using var ftpResponse = (FtpWebResponse)await ftpRequest.GetResponseAsync().WaitAsync(cancellationToken);
            _ = ftpResponse.StatusDescription;

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

            var extension = NormalizeExtension(request.Extension);
            string fileName = $"{Guid.NewGuid():N}{extension}";
            var relativePath = BuildRelativePath(request.Folder, fileName);

            await EnsureDirectoriesExistAsync(request.Folder, cancellationToken);

            byte[] fileContents = Convert.FromBase64String(request.Base64Content);
            var ftpUri = BuildFtpUri(relativePath);
            var ftpRequest = CreateFtpRequest(ftpUri, WebRequestMethods.Ftp.UploadFile);

            await using (var ftpStream = await ftpRequest.GetRequestStreamAsync().WaitAsync(cancellationToken))
            {
                await ftpStream.WriteAsync(fileContents, cancellationToken);
            }

            using var ftpResponse = (FtpWebResponse)await ftpRequest.GetResponseAsync().WaitAsync(cancellationToken);
            _ = ftpResponse.StatusDescription;

            return new FileUploadResult
            {
                Extension = extension,
                SizeBytes = fileContents.LongLength,
                Path = BuildPublicUrl(relativePath)
            };
        }

        private FtpWebRequest CreateFtpRequest(Uri uri, string method)
        {
#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = method;
            request.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            request.UseBinary = true;
            request.KeepAlive = false;
            return request;
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

        private async Task EnsureDirectoriesExistAsync(string folder, CancellationToken cancellationToken)
        {
            var folderPath = (folder ?? string.Empty).Trim('/');
            if (string.IsNullOrEmpty(folderPath)) return;

            var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = string.Empty;

            foreach (var segment in segments)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
                var dirUri = BuildFtpUri(currentPath);

                try
                {
                    var mkDirRequest = CreateFtpRequest(dirUri, WebRequestMethods.Ftp.MakeDirectory);
                    using var response = (FtpWebResponse)await mkDirRequest.GetResponseAsync().WaitAsync(cancellationToken);
                    _ = response.StatusDescription;
                }
                catch (WebException ex) when (ex.Response is FtpWebResponse ftpResponse)
                {
                    if (ftpResponse.StatusCode != FtpStatusCode.ActionNotTakenFilenameNotAllowed &&
                        ftpResponse.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        var message = ftpResponse.StatusDescription ?? ex.Message;
                        if (!message.Contains("exist", StringComparison.OrdinalIgnoreCase) &&
                            !message.Contains("already", StringComparison.OrdinalIgnoreCase))
                            throw;
                    }
                }
            }
        }

        private Uri BuildFtpUri(string relativePath)
        {
            var baseUri = EnsureFtpBaseUri();
            relativePath = (relativePath ?? string.Empty).TrimStart('/');
            return new Uri(baseUri, relativePath);
        }

        private Uri ToFtpUri(string pathOrUrl)
        {
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute))
            {
                if (absolute.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
                    return absolute;
                return BuildFtpUri(absolute.AbsolutePath.TrimStart('/'));
            }
            return BuildFtpUri(pathOrUrl.TrimStart('/'));
        }

        private string BuildPublicUrl(string relativePath)
        {
            var host = GetHost();
            return $"https://{host.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }

        private string GetHost()
        {
            if (string.IsNullOrWhiteSpace(_settings.FtpAddress)) return string.Empty;
            var addr = _settings.FtpAddress.Trim();
            if (Uri.TryCreate(addr, UriKind.Absolute, out var uri))
                return uri.Host;
            if (Uri.TryCreate("ftp://" + addr.TrimStart('/'), UriKind.Absolute, out uri))
                return uri.Host;
            return addr.Split('/')[0];
        }

        private Uri EnsureFtpBaseUri()
        {
            if (string.IsNullOrWhiteSpace(_settings.FtpAddress))
                throw new InvalidOperationException($"{nameof(FtpSettings)}:{nameof(FtpSettings.FtpAddress)} gerekli.");

            var address = _settings.FtpAddress.Trim().TrimEnd('/') + "/";

            if (Uri.TryCreate(address, UriKind.Absolute, out var absolute))
            {
                if (absolute.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
                    return absolute;
                throw new InvalidOperationException($"FtpAddress FTP adresi olmalı (ftp://...).");
            }

            if (Uri.TryCreate("ftp://" + address.TrimStart('/'), UriKind.Absolute, out absolute))
                return absolute;

            throw new InvalidOperationException($"Geçersiz FtpAddress: '{_settings.FtpAddress}'");
        }
    }
}
