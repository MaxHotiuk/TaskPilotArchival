namespace Core.Models;

public class BlobFileMetadata
{
    public string FileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}
