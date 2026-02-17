namespace ProjectBase.Core.FileService.Models
{
    public sealed class Base64UploadRequest
    {
        public required string Base64Content { get; init; }
        public required string Folder { get; init; }
        public string? Extension { get; init; }
    }
}

