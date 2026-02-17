namespace ProjectBase.Core.FileService.Models
{
    public sealed class FileUploadResult
    {
        public required string Path { get; init; }
        public required string Extension { get; init; }
        public long SizeBytes { get; init; }

        public decimal SizeKb => SizeBytes / 1024m;
    }
}

