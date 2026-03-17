using System.Text.Json.Serialization;

namespace Redact1.Models
{
    public class Agency
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("api_base_url")]
        public string? ApiBaseUrl { get; set; }

        [JsonPropertyName("login_identifiers")]
        public string LoginIdentifiers { get; set; } = "email";

        [JsonPropertyName("primary_color")]
        public string? PrimaryColor { get; set; }

        [JsonPropertyName("support_email")]
        public string? SupportEmail { get; set; }

        [JsonPropertyName("support_phone")]
        public string? SupportPhone { get; set; }

        public bool SupportsEmail => LoginIdentifiers.Contains("email");
        public bool SupportsEmployeeId => LoginIdentifiers.Contains("employeeId");
    }

    public class AgencyConfig
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public List<string> LoginIdentifiers { get; set; } = new() { "email" };
        public string? PrimaryColor { get; set; }
        public string? SupportEmail { get; set; }
        public string? SupportPhone { get; set; }
    }
}
