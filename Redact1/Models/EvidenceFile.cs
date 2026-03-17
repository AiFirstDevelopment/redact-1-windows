using System.Text.Json.Serialization;

namespace Redact1.Models
{
    public class EvidenceFile
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty;

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("original_r2_key")]
        public string OriginalR2Key { get; set; } = string.Empty;

        [JsonPropertyName("redacted_r2_key")]
        public string? RedactedR2Key { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "uploaded";

        [JsonPropertyName("uploaded_by")]
        public string UploadedBy { get; set; } = string.Empty;

        [JsonPropertyName("deleted_at")]
        public long? DeletedAt { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }

        public bool IsImage => FileType == "image";
        public bool IsPdf => FileType == "pdf";

        public string FileSizeDisplay
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            }
        }
    }

    public enum FileStatus
    {
        Uploaded,
        Processing,
        Detected,
        Reviewed,
        Exported
    }

    public class FileUploadResponse
    {
        [JsonPropertyName("file")]
        public EvidenceFile File { get; set; } = new();
    }
}
