using System.Text.Json.Serialization;

namespace Redact1.Models
{
    public class Export
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("r2_key")]
        public string R2Key { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("file_count")]
        public int FileCount { get; set; }

        [JsonPropertyName("exported_by")]
        public string ExportedBy { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).LocalDateTime;
    }

    public class AuditLog
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).LocalDateTime;

        public string ActionDisplay => Action switch
        {
            "create" => "Created",
            "update" => "Updated",
            "delete" => "Deleted",
            "upload" => "Uploaded",
            "download" => "Downloaded",
            "export" => "Exported",
            "approve" => "Approved",
            "reject" => "Rejected",
            "archive" => "Archived",
            "unarchive" => "Unarchived",
            _ => Action
        };
    }
}
