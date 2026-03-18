using Redact1.Models;

namespace Redact1.Services
{
    public interface IApiService
    {
        void SetAuthToken(string? token);

        // Auth
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task LogoutAsync();
        Task<User> GetCurrentUserAsync();

        // Users
        Task<List<User>> GetUsersAsync();
        Task<User> CreateUserAsync(CreateUserRequest request);
        Task<User> GetUserAsync(string id);
        Task<User> UpdateUserAsync(string id, UpdateUserRequest request);
        Task DeleteUserAsync(string id);
        Task<List<AuditLog>> GetUserAuditAsync(string userId);

        // Requests
        Task<List<RecordsRequest>> GetRequestsAsync(string? status = null, string? search = null);
        Task<List<RecordsRequest>> GetArchivedRequestsAsync(string? search = null);
        Task<RecordsRequest> CreateRequestAsync(CreateRequestPayload request);
        Task<RecordsRequest> GetRequestAsync(string id);
        Task<RecordsRequest> UpdateRequestAsync(string id, UpdateRequestPayload request);
        Task DeleteRequestAsync(string id);
        Task<RecordsRequest> ArchiveRequestAsync(string id);
        Task<RecordsRequest> UnarchiveRequestAsync(string id);
        Task<List<AuditLog>> GetRequestAuditAsync(string requestId);

        // Files
        Task<List<EvidenceFile>> GetFilesAsync(string requestId);
        Task<EvidenceFile> UploadFileAsync(string requestId, string filePath);
        Task<EvidenceFile> GetFileAsync(string id);
        Task<byte[]> GetOriginalFileAsync(string fileId);
        Task<byte[]> GetRedactedFileAsync(string fileId);
        Task<EvidenceFile> UploadRedactedFileAsync(string fileId, byte[] fileData, string filename);
        Task DeleteFileAsync(string id);
        Task<List<AuditLog>> GetFileAuditAsync(string fileId);

        // Detections
        Task<DetectionListResponse> GetDetectionsAsync(string fileId);
        Task<List<Detection>> CreateDetectionsAsync(string fileId, List<CreateDetectionRequest> detections);
        Task ClearDetectionsAsync(string fileId);
        Task<Detection> UpdateDetectionAsync(string detectionId, UpdateDetectionRequest request);
        Task<ManualRedaction> CreateManualRedactionAsync(string fileId, CreateManualRedactionRequest request);
        Task<ManualRedaction> UpdateManualRedactionAsync(string redactionId, UpdateManualRedactionRequest request);
        Task DeleteManualRedactionAsync(string redactionId);

        // Exports
        Task<Export> CreateExportAsync(string requestId);
        Task<List<Export>> GetExportsAsync(string requestId);
        Task<byte[]> DownloadExportAsync(string exportId);

        // Agencies
        Task<List<Agency>> GetAgenciesAsync();
        Task<Agency?> GetAgencyByCodeAsync(string code);
        Task<Agency?> GetAgencyByDomainAsync(string domain);
    }
}
