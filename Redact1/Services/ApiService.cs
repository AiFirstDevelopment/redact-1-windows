using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Redact1.Models;

namespace Redact1.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private string? _authToken;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void SetAuthToken(string? token)
        {
            _authToken = token;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"/api{endpoint}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>()
                   ?? throw new Exception("Failed to deserialize response");
        }

        private async Task<T> PostAsync<T>(string endpoint, object? data = null)
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                : null;
            var response = await _httpClient.PostAsync($"/api{endpoint}", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[API] Error {response.StatusCode}: {errorBody}");
                response.EnsureSuccessStatusCode();
            }
            return await response.Content.ReadFromJsonAsync<T>()
                   ?? throw new Exception("Failed to deserialize response");
        }

        private async Task PostAsync(string endpoint, object? data = null)
        {
            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                : null;
            var response = await _httpClient.PostAsync($"/api{endpoint}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<T> PutAsync<T>(string endpoint, object data)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api{endpoint}", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>()
                   ?? throw new Exception("Failed to deserialize response");
        }

        private async Task DeleteAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync($"/api{endpoint}");
            response.EnsureSuccessStatusCode();
        }

        // Auth
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            return await PostAsync<LoginResponse>("/auth/login", request);
        }

        public async Task LogoutAsync()
        {
            await PostAsync("/auth/logout");
        }

        public async Task<User> GetCurrentUserAsync()
        {
            return await GetAsync<User>("/auth/me");
        }

        // Users
        public async Task<List<User>> GetUsersAsync()
        {
            var response = await GetAsync<UsersListResponse>("/users");
            return response.Users;
        }

        public async Task<User> CreateUserAsync(CreateUserRequest request)
        {
            return await PostAsync<User>("/users", request);
        }

        public async Task<User> GetUserAsync(string id)
        {
            return await GetAsync<User>($"/users/{id}");
        }

        public async Task<User> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            return await PutAsync<User>($"/users/{id}", request);
        }

        public async Task DeleteUserAsync(string id)
        {
            await DeleteAsync($"/users/{id}");
        }

        public async Task<List<AuditLog>> GetUserAuditAsync(string userId)
        {
            return await GetAsync<List<AuditLog>>($"/users/{userId}/audit");
        }

        // Requests
        public async Task<List<RecordsRequest>> GetRequestsAsync(string? status = null, string? search = null)
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(status)) query.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";
            var response = await GetAsync<RequestsListResponse>($"/requests{queryString}");
            return response.Requests;
        }

        public async Task<List<RecordsRequest>> GetArchivedRequestsAsync(string? search = null)
        {
            var queryString = !string.IsNullOrEmpty(search) ? $"?search={Uri.EscapeDataString(search)}" : "";
            var response = await GetAsync<RequestsListResponse>($"/requests/archived{queryString}");
            return response.Requests;
        }

        public async Task<RecordsRequest> CreateRequestAsync(CreateRequestPayload request)
        {
            var response = await PostAsync<RequestResponse>("/requests", request);
            return response.Request;
        }

        public async Task<RecordsRequest> GetRequestAsync(string id)
        {
            var response = await GetAsync<RequestResponse>($"/requests/{id}");
            return response.Request;
        }

        public async Task<RecordsRequest> UpdateRequestAsync(string id, UpdateRequestPayload request)
        {
            var response = await PutAsync<RequestResponse>($"/requests/{id}", request);
            return response.Request;
        }

        public async Task DeleteRequestAsync(string id)
        {
            await DeleteAsync($"/requests/{id}");
        }

        public async Task<RecordsRequest> ArchiveRequestAsync(string id)
        {
            var response = await PostAsync<RequestResponse>($"/requests/{id}/archive");
            return response.Request;
        }

        public async Task<RecordsRequest> UnarchiveRequestAsync(string id)
        {
            return await PostAsync<RecordsRequest>($"/requests/{id}/unarchive");
        }

        public async Task<List<AuditLog>> GetRequestAuditAsync(string requestId)
        {
            return await GetAsync<List<AuditLog>>($"/requests/{requestId}/audit");
        }

        // Files
        public async Task<List<EvidenceFile>> GetFilesAsync(string requestId)
        {
            var response = await GetAsync<FilesListResponse>($"/requests/{requestId}/files");
            return response.Files;
        }

        public async Task<EvidenceFile> UploadFileAsync(string requestId, string filePath)
        {
            Console.WriteLine($"[API] Uploading to /api/requests/{requestId}/files");
            Console.WriteLine($"[API] Auth header: {_httpClient.DefaultRequestHeaders.Authorization}");

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var filename = Path.GetFileName(filePath);
            var mimeType = GetMimeType(filePath);
            var boundary = Guid.NewGuid().ToString();

            Console.WriteLine($"[API] File: {filename}, Size: {fileBytes.Length}, MimeType: {mimeType}");

            // Build multipart body manually like iOS
            using var body = new MemoryStream();
            using var writer = new StreamWriter(body, Encoding.UTF8, leaveOpen: true);

            await writer.WriteAsync($"--{boundary}\r\n");
            await writer.WriteAsync($"Content-Disposition: form-data; name=\"file\"; filename=\"{filename}\"\r\n");
            await writer.WriteAsync($"Content-Type: {mimeType}\r\n\r\n");
            await writer.FlushAsync();
            await body.WriteAsync(fileBytes);
            await writer.WriteAsync($"\r\n--{boundary}--\r\n");
            await writer.FlushAsync();

            body.Position = 0;
            var content = new StreamContent(body);
            content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));

            var response = await _httpClient.PostAsync($"/api/requests/{requestId}/files", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[API] Response: {response.StatusCode} - {responseBody}");

            response.EnsureSuccessStatusCode();
            var result = JsonSerializer.Deserialize<FileUploadResponse>(responseBody)
                         ?? throw new Exception("Failed to deserialize response");
            return result.File;
        }

        public async Task<EvidenceFile> GetFileAsync(string id)
        {
            var response = await GetAsync<FileUploadResponse>($"/files/{id}");
            return response.File;
        }

        public async Task<byte[]> GetOriginalFileAsync(string fileId)
        {
            var response = await _httpClient.GetAsync($"/api/files/{fileId}/original");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<byte[]> GetRedactedFileAsync(string fileId)
        {
            var response = await _httpClient.GetAsync($"/api/files/{fileId}/redacted");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<EvidenceFile> UploadRedactedFileAsync(string fileId, byte[] fileData, string filename)
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileData);
            var mimeType = GetMimeType(filename);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            form.Add(fileContent, "file", filename);

            var response = await _httpClient.PostAsync($"/api/files/{fileId}/redacted", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EvidenceFile>()
                   ?? throw new Exception("Failed to deserialize response");
        }

        public async Task DeleteFileAsync(string id)
        {
            await DeleteAsync($"/files/{id}");
        }

        public async Task<List<AuditLog>> GetFileAuditAsync(string fileId)
        {
            return await GetAsync<List<AuditLog>>($"/files/{fileId}/audit");
        }

        // Detections
        public async Task<DetectionListResponse> GetDetectionsAsync(string fileId)
        {
            return await GetAsync<DetectionListResponse>($"/files/{fileId}/detections");
        }

        public async Task<List<Detection>> CreateDetectionsAsync(string fileId, List<CreateDetectionRequest> detections)
        {
            var response = await PostAsync<DetectionListResponse>($"/files/{fileId}/detections", new { detections });
            return response.Detections;
        }

        public async Task ClearDetectionsAsync(string fileId)
        {
            await DeleteAsync($"/files/{fileId}/detections");
        }

        public async Task<Detection> UpdateDetectionAsync(string detectionId, UpdateDetectionRequest request)
        {
            return await PutAsync<Detection>($"/detections/{detectionId}", request);
        }

        public async Task<ManualRedaction> CreateManualRedactionAsync(string fileId, CreateManualRedactionRequest request)
        {
            return await PostAsync<ManualRedaction>($"/files/{fileId}/manual-redactions", request);
        }

        public async Task<ManualRedaction> UpdateManualRedactionAsync(string redactionId, UpdateManualRedactionRequest request)
        {
            return await PutAsync<ManualRedaction>($"/manual-redactions/{redactionId}", request);
        }

        public async Task DeleteManualRedactionAsync(string redactionId)
        {
            await DeleteAsync($"/manual-redactions/{redactionId}");
        }

        // Exports
        public async Task<Export> CreateExportAsync(string requestId)
        {
            return await PostAsync<Export>($"/requests/{requestId}/export");
        }

        public async Task<List<Export>> GetExportsAsync(string requestId)
        {
            return await GetAsync<List<Export>>($"/requests/{requestId}/exports");
        }

        public async Task<byte[]> DownloadExportAsync(string exportId)
        {
            var response = await _httpClient.GetAsync($"/api/exports/{exportId}/download");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // Agencies
        public async Task<List<Agency>> GetAgenciesAsync()
        {
            return await GetAsync<List<Agency>>("/agencies");
        }

        public async Task<Agency?> GetAgencyByCodeAsync(string code)
        {
            try
            {
                return await GetAsync<Agency>($"/agencies/code/{code}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<Agency?> GetAgencyByDomainAsync(string domain)
        {
            try
            {
                return await GetAsync<Agency>($"/agencies/domain/{domain}");
            }
            catch
            {
                return null;
            }
        }

        private static string GetMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}
