using System.Text.Json.Serialization;

namespace Redact1.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "clerk";

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }

        public bool IsSupervisor => Role == "supervisor";
    }

    public class LoginRequest
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public User User { get; set; } = new();
    }

    public class CreateUserRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "clerk";
    }

    public class UpdateUserRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }
}
