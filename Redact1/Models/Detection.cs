using System.Text.Json.Serialization;

namespace Redact1.Models
{
    public class Detection
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("detection_type")]
        public string DetectionType { get; set; } = string.Empty;

        [JsonPropertyName("bbox_x")]
        public double? BboxX { get; set; }

        [JsonPropertyName("bbox_y")]
        public double? BboxY { get; set; }

        [JsonPropertyName("bbox_width")]
        public double? BboxWidth { get; set; }

        [JsonPropertyName("bbox_height")]
        public double? BboxHeight { get; set; }

        [JsonPropertyName("page_number")]
        public int? PageNumber { get; set; }

        [JsonPropertyName("text_start")]
        public int? TextStart { get; set; }

        [JsonPropertyName("text_end")]
        public int? TextEnd { get; set; }

        [JsonPropertyName("text_content")]
        public string? TextContent { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending";

        [JsonPropertyName("reviewed_by")]
        public string? ReviewedBy { get; set; }

        [JsonPropertyName("reviewed_at")]
        public long? ReviewedAt { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        public bool HasBoundingBox => BboxX.HasValue && BboxY.HasValue && BboxWidth.HasValue && BboxHeight.HasValue;

        public string DisplayName => DetectionType switch
        {
            "face" => "Face",
            "plate" => "License Plate",
            "ssn" => "SSN",
            "phone" => "Phone Number",
            "email" => "Email Address",
            "address" => "Address",
            "dob" => "Date of Birth",
            _ => DetectionType
        };

        public string ConfidenceDisplay => Confidence.HasValue ? $"{Confidence.Value * 100:F0}%" : "N/A";
    }

    public class ManualRedaction
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("redaction_type")]
        public string RedactionType { get; set; } = "manual";

        [JsonPropertyName("bbox_x")]
        public double? BboxX { get; set; }

        [JsonPropertyName("bbox_y")]
        public double? BboxY { get; set; }

        [JsonPropertyName("bbox_width")]
        public double? BboxWidth { get; set; }

        [JsonPropertyName("bbox_height")]
        public double? BboxHeight { get; set; }

        [JsonPropertyName("page_number")]
        public int? PageNumber { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }
    }

    public class DetectionListResponse
    {
        [JsonPropertyName("detections")]
        public List<Detection> Detections { get; set; } = new();

        [JsonPropertyName("manual_redactions")]
        public List<ManualRedaction> ManualRedactions { get; set; } = new();
    }

    public class CreateDetectionRequest
    {
        [JsonPropertyName("detection_type")]
        public string DetectionType { get; set; } = string.Empty;

        [JsonPropertyName("bbox_x")]
        public double? BboxX { get; set; }

        [JsonPropertyName("bbox_y")]
        public double? BboxY { get; set; }

        [JsonPropertyName("bbox_width")]
        public double? BboxWidth { get; set; }

        [JsonPropertyName("bbox_height")]
        public double? BboxHeight { get; set; }

        [JsonPropertyName("page_number")]
        public int? PageNumber { get; set; }

        [JsonPropertyName("text_start")]
        public int? TextStart { get; set; }

        [JsonPropertyName("text_end")]
        public int? TextEnd { get; set; }

        [JsonPropertyName("text_content")]
        public string? TextContent { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }
    }

    public class UpdateDetectionRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class CreateManualRedactionRequest
    {
        [JsonPropertyName("redaction_type")]
        public string RedactionType { get; set; } = "manual";

        [JsonPropertyName("bbox_x")]
        public double BboxX { get; set; }

        [JsonPropertyName("bbox_y")]
        public double BboxY { get; set; }

        [JsonPropertyName("bbox_width")]
        public double BboxWidth { get; set; }

        [JsonPropertyName("bbox_height")]
        public double BboxHeight { get; set; }

        [JsonPropertyName("page_number")]
        public int? PageNumber { get; set; }
    }
}
