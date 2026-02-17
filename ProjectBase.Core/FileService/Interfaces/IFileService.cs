using ProjectBase.Core.FileService.Models;

namespace ProjectBase.Core.FileService.Interfaces
{
    public interface IFileService
    {
        Task<FileUploadResult> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);
        Task<FileUploadResult> UploadBase64Async(Base64UploadRequest request, CancellationToken cancellationToken = default);
        Task RemoveAsync(string pathOrUrl, CancellationToken cancellationToken = default);
    }
}
