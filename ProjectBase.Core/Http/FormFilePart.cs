namespace ProjectBase.Core.Http;

public sealed record FormFilePart(
    string Name,
    Stream Content,
    string FileName,
    string? ContentType = null,
    bool LeaveOpen = false);


