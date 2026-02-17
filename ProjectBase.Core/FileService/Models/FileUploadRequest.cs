using Microsoft.AspNetCore.Http;

namespace ProjectBase.Core.FileService.Models
{
    public sealed class FileUploadRequest
    {
        public required IFormFile File { get; init; }
        public required string Folder { get; init; }
    }
}

