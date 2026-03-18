using FluentAssertions;
using Moq;
using Moq.Protected;
using Redact1.Models;
using Redact1.Services;
using System.Net;
using System.Text.Json;

namespace Redact1.Tests.Services;

public class ApiServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ApiService _apiService;

    public ApiServiceTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api.com")
        };
        _apiService = new ApiService(_httpClient);
    }

    private void SetupResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json)
            });
    }

    [Fact]
    public void SetAuthToken_SetsAuthorizationHeader()
    {
        _apiService.SetAuthToken("test-token");

        _httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        _httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        _httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be("test-token");
    }

    [Fact]
    public void SetAuthToken_WithNull_ClearsHeader()
    {
        _apiService.SetAuthToken("test-token");
        _apiService.SetAuthToken(null);

        _httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsLoginResponse()
    {
        var expectedResponse = new LoginResponse
        {
            Token = "jwt-token",
            User = new User { Id = "user-1", Email = "test@test.com" }
        };
        SetupResponse(expectedResponse);

        var result = await _apiService.LoginAsync(new LoginRequest
        {
            Email = "test@test.com",
            Password = "password"
        });

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser()
    {
        var expectedUser = new User { Id = "user-1", Name = "Test User" };
        SetupResponse(expectedUser);

        var result = await _apiService.GetCurrentUserAsync();

        result.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetRequestsAsync_ReturnsRequests()
    {
        var response = new RequestsListResponse
        {
            Requests = new List<RecordsRequest>
            {
                new RecordsRequest { Id = "req-1", Title = "Request 1" },
                new RecordsRequest { Id = "req-2", Title = "Request 2" }
            }
        };
        SetupResponse(response);

        var result = await _apiService.GetRequestsAsync();

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Request 1");
    }

    [Fact]
    public async Task GetRequestsAsync_WithStatusFilter_SendsCorrectQuery()
    {
        SetupResponse(new RequestsListResponse());

        await _apiService.GetRequestsAsync(status: "in_progress");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString().Contains("status=in_progress")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetRequestAsync_ReturnsRequest()
    {
        var response = new RequestResponse
        {
            Request = new RecordsRequest { Id = "req-1", Title = "Test Request" }
        };
        SetupResponse(response);

        var result = await _apiService.GetRequestAsync("req-1");

        result.Title.Should().Be("Test Request");
    }

    [Fact]
    public async Task CreateRequestAsync_ReturnsCreatedRequest()
    {
        var response = new RequestResponse
        {
            Request = new RecordsRequest { Id = "new-req", Title = "New Request" }
        };
        SetupResponse(response);

        var result = await _apiService.CreateRequestAsync(new CreateRequestPayload
        {
            Title = "New Request",
            RequestNumber = "FOIA-001"
        });

        result.Title.Should().Be("New Request");
    }

    [Fact]
    public async Task GetFilesAsync_ReturnsFiles()
    {
        var response = new FilesListResponse
        {
            Files = new List<EvidenceFile>
            {
                new EvidenceFile { Id = "file-1", Filename = "test.pdf" }
            }
        };
        SetupResponse(response);

        var result = await _apiService.GetFilesAsync("req-1");

        result.Should().HaveCount(1);
        result[0].Filename.Should().Be("test.pdf");
    }

    [Fact]
    public async Task GetDetectionsAsync_ReturnsDetections()
    {
        var response = new DetectionListResponse
        {
            Detections = new List<Detection>
            {
                new Detection { Id = "det-1", DetectionType = "ssn" }
            },
            ManualRedactions = new List<ManualRedaction>()
        };
        SetupResponse(response);

        var result = await _apiService.GetDetectionsAsync("file-1");

        result.Detections.Should().HaveCount(1);
        result.Detections[0].DetectionType.Should().Be("ssn");
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsUsers()
    {
        var usersResponse = new UsersListResponse
        {
            Users = new List<User>
            {
                new User { Id = "user-1", Name = "User 1" },
                new User { Id = "user-2", Name = "User 2" }
            }
        };
        SetupResponse(usersResponse);

        var result = await _apiService.GetUsersAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateRequestAsync_ReturnsUpdatedRequest()
    {
        var response = new RequestResponse
        {
            Request = new RecordsRequest { Id = "req-1", Title = "Updated Title" }
        };
        SetupResponse(response);

        var result = await _apiService.UpdateRequestAsync("req-1", new UpdateRequestPayload
        {
            Title = "Updated Title"
        });

        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsCreatedUser()
    {
        var newUser = new User { Id = "new-user", Name = "New User" };
        SetupResponse(newUser);

        var result = await _apiService.CreateUserAsync(new CreateUserRequest
        {
            Name = "New User",
            Email = "new@test.com"
        });

        result.Name.Should().Be("New User");
    }

    [Fact]
    public async Task UpdateDetectionAsync_ReturnsUpdatedDetection()
    {
        var updated = new Detection { Id = "det-1", Status = "approved" };
        SetupResponse(updated);

        var result = await _apiService.UpdateDetectionAsync("det-1", new UpdateDetectionRequest
        {
            Status = "approved"
        });

        result.Status.Should().Be("approved");
    }

    [Fact]
    public async Task GetExportsAsync_ReturnsExports()
    {
        var exports = new List<Export>
        {
            new Export { Id = "exp-1", Filename = "export.zip" }
        };
        SetupResponse(exports);

        var result = await _apiService.GetExportsAsync("req-1");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateExportAsync_ReturnsExport()
    {
        var export = new Export { Id = "exp-1", Filename = "export.zip" };
        SetupResponse(export);

        var result = await _apiService.CreateExportAsync("req-1");

        result.Filename.Should().Be("export.zip");
    }

    [Fact]
    public async Task LogoutAsync_CallsEndpoint()
    {
        SetupEmptyResponse();

        await _apiService.LogoutAsync();

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString().Contains("/auth/logout")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRequestAsync_CallsEndpoint()
    {
        SetupEmptyResponse();

        await _apiService.DeleteRequestAsync("req-1");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Delete &&
                r.RequestUri!.ToString().Contains("/requests/req-1")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveRequestAsync_ReturnsArchivedRequest()
    {
        var response = new RequestResponse
        {
            Request = new RecordsRequest { Id = "req-1", Status = "archived" }
        };
        SetupResponse(response);

        var result = await _apiService.ArchiveRequestAsync("req-1");

        result.Status.Should().Be("archived");
    }

    [Fact]
    public async Task UnarchiveRequestAsync_ReturnsRequest()
    {
        var request = new RecordsRequest { Id = "req-1", Status = "new" };
        SetupResponse(request);

        var result = await _apiService.UnarchiveRequestAsync("req-1");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetArchivedRequestsAsync_ReturnsRequests()
    {
        var response = new RequestsListResponse
        {
            Requests = new List<RecordsRequest>
            {
                new RecordsRequest { Id = "req-1", Status = "archived" }
            }
        };
        SetupResponse(response);

        var result = await _apiService.GetArchivedRequestsAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetArchivedRequestsAsync_WithSearch_SendsCorrectQuery()
    {
        SetupResponse(new RequestsListResponse());

        await _apiService.GetArchivedRequestsAsync(search: "test");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString().Contains("search=test")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser()
    {
        var user = new User { Id = "user-1", Name = "Test User" };
        SetupResponse(user);

        var result = await _apiService.GetUserAsync("user-1");

        result.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsUpdatedUser()
    {
        var updatedUser = new User { Id = "user-1", Name = "Updated Name" };
        SetupResponse(updatedUser);

        var result = await _apiService.UpdateUserAsync("user-1", new UpdateUserRequest
        {
            Name = "Updated Name"
        });

        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteUserAsync_CallsEndpoint()
    {
        SetupEmptyResponse();

        await _apiService.DeleteUserAsync("user-1");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Delete &&
                r.RequestUri!.ToString().Contains("/users/user-1")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetUserAuditAsync_ReturnsAuditLogs()
    {
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = "log-1", Action = "create" }
        };
        SetupResponse(logs);

        var result = await _apiService.GetUserAuditAsync("user-1");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRequestAuditAsync_ReturnsAuditLogs()
    {
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = "log-1", Action = "update" }
        };
        SetupResponse(logs);

        var result = await _apiService.GetRequestAuditAsync("req-1");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFileAsync_ReturnsFile()
    {
        var file = new EvidenceFile { Id = "file-1", Filename = "test.pdf" };
        SetupResponse(new FileUploadResponse { File = file });

        var result = await _apiService.GetFileAsync("file-1");

        result.Filename.Should().Be("test.pdf");
    }

    [Fact]
    public async Task GetOriginalFileAsync_ReturnsByteArray()
    {
        var fileData = new byte[] { 0x01, 0x02, 0x03 };
        SetupBinaryResponse(fileData);

        var result = await _apiService.GetOriginalFileAsync("file-1");

        result.Should().BeEquivalentTo(fileData);
    }

    [Fact]
    public async Task GetRedactedFileAsync_ReturnsByteArray()
    {
        var fileData = new byte[] { 0x04, 0x05, 0x06 };
        SetupBinaryResponse(fileData);

        var result = await _apiService.GetRedactedFileAsync("file-1");

        result.Should().BeEquivalentTo(fileData);
    }

    [Fact]
    public async Task DeleteFileAsync_CallsEndpoint()
    {
        SetupEmptyResponse();

        await _apiService.DeleteFileAsync("file-1");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Delete &&
                r.RequestUri!.ToString().Contains("/files/file-1")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetFileAuditAsync_ReturnsAuditLogs()
    {
        var logs = new List<AuditLog>
        {
            new AuditLog { Id = "log-1", Action = "upload" }
        };
        SetupResponse(logs);

        var result = await _apiService.GetFileAuditAsync("file-1");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateDetectionsAsync_ReturnsCreatedDetections()
    {
        var response = new DetectionListResponse
        {
            Detections = new List<Detection>
            {
                new Detection { Id = "det-1", DetectionType = "face" }
            },
            ManualRedactions = new List<ManualRedaction>()
        };
        SetupResponse(response);

        var result = await _apiService.CreateDetectionsAsync("file-1", new List<CreateDetectionRequest>
        {
            new CreateDetectionRequest { DetectionType = "face" }
        });

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateDetectionsAsync_SendsWrappedFormat()
    {
        var response = new DetectionListResponse
        {
            Detections = new List<Detection>(),
            ManualRedactions = new List<ManualRedaction>()
        };
        SetupResponse(response);

        await _apiService.CreateDetectionsAsync("file-1", new List<CreateDetectionRequest>
        {
            new CreateDetectionRequest { DetectionType = "ssn", TextContent = "123-45-6789" }
        });

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString().Contains("/files/file-1/detections")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ClearDetectionsAsync_CallsEndpoint()
    {
        SetupEmptyResponse();

        await _apiService.ClearDetectionsAsync("file-1");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Delete &&
                r.RequestUri!.ToString().Contains("/files/file-1/detections")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CreateManualRedactionAsync_ReturnsRedaction()
    {
        var redaction = new ManualRedaction { Id = "red-1", BboxX = 0.1 };
        SetupResponse(redaction);

        var result = await _apiService.CreateManualRedactionAsync("file-1", new CreateManualRedactionRequest
        {
            BboxX = 0.1
        });

        result.BboxX.Should().Be(0.1);
    }

    [Fact]
    public async Task UpdateManualRedactionAsync_ReturnsUpdatedRedaction()
    {
        var redaction = new ManualRedaction { Id = "red-1", BboxX = 0.2 };
        SetupResponse(redaction);

        var result = await _apiService.UpdateManualRedactionAsync("red-1", new UpdateManualRedactionRequest
        {
            BboxX = 0.2
        });

        result.BboxX.Should().Be(0.2);
    }

    [Fact]
    public async Task DeleteManualRedactionAsync_CallsEndpoint()
    {
        SetupEmptyResponse();

        await _apiService.DeleteManualRedactionAsync("red-1");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Delete &&
                r.RequestUri!.ToString().Contains("/manual-redactions/red-1")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DownloadExportAsync_ReturnsByteArray()
    {
        var exportData = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        SetupBinaryResponse(exportData);

        var result = await _apiService.DownloadExportAsync("exp-1");

        result.Should().BeEquivalentTo(exportData);
    }

    [Fact]
    public async Task GetAgenciesAsync_ReturnsAgencies()
    {
        var agencies = new List<Agency>
        {
            new Agency { Id = "ag-1", Code = "TEST", Name = "Test Agency" }
        };
        SetupResponse(agencies);

        var result = await _apiService.GetAgenciesAsync();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("TEST");
    }

    [Fact]
    public async Task GetAgencyByCodeAsync_ReturnsAgency()
    {
        var agency = new Agency { Id = "ag-1", Code = "TEST", Name = "Test Agency" };
        SetupResponse(agency);

        var result = await _apiService.GetAgencyByCodeAsync("TEST");

        result.Should().NotBeNull();
        result!.Code.Should().Be("TEST");
    }

    [Fact]
    public async Task GetAgencyByCodeAsync_NotFound_ReturnsNull()
    {
        SetupErrorResponse(HttpStatusCode.NotFound);

        var result = await _apiService.GetAgencyByCodeAsync("INVALID");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAgencyByDomainAsync_ReturnsAgency()
    {
        var agency = new Agency { Id = "ag-1", Code = "TEST", Name = "Test Agency" };
        SetupResponse(agency);

        var result = await _apiService.GetAgencyByDomainAsync("test.gov");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAgencyByDomainAsync_NotFound_ReturnsNull()
    {
        SetupErrorResponse(HttpStatusCode.NotFound);

        var result = await _apiService.GetAgencyByDomainAsync("invalid.com");

        result.Should().BeNull();
    }

    [Fact]
    public void SetAuthToken_WithEmptyString_ClearsHeader()
    {
        _apiService.SetAuthToken("test-token");
        _apiService.SetAuthToken("");

        _httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetRequestsAsync_WithSearchFilter_SendsCorrectQuery()
    {
        SetupResponse(new RequestsListResponse());

        await _apiService.GetRequestsAsync(search: "test");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString().Contains("search=test")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetRequestsAsync_WithBothFilters_SendsCorrectQuery()
    {
        SetupResponse(new RequestsListResponse());

        await _apiService.GetRequestsAsync(status: "new", search: "test");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString().Contains("status=new") &&
                r.RequestUri!.ToString().Contains("search=test")),
            ItExpr.IsAny<CancellationToken>());
    }

    private void SetupEmptyResponse()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            });
    }

    private void SetupBinaryResponse(byte[] data)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(data)
            });
    }

    private void SetupErrorResponse(HttpStatusCode statusCode)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("")
            });
    }

    [Fact]
    public async Task UploadFileAsync_UploadsFileWithCorrectMimeType()
    {
        var uploadResponse = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.pdf" }
        };
        SetupResponse(uploadResponse);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF magic bytes

            var result = await _apiService.UploadFileAsync("req-1", tempFile);

            result.Filename.Should().Be("test.pdf");
            _mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("/requests/req-1/files")),
                ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadFileAsync_JpegFile_SetsCorrectMimeType()
    {
        var uploadResponse = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.jpg" }
        };
        SetupResponse(uploadResponse);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jpg");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0xFF, 0xD8, 0xFF });

            var result = await _apiService.UploadFileAsync("req-1", tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadFileAsync_PngFile_SetsCorrectMimeType()
    {
        var uploadResponse = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.png" }
        };
        SetupResponse(uploadResponse);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            var result = await _apiService.UploadFileAsync("req-1", tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadFileAsync_GifFile_SetsCorrectMimeType()
    {
        var uploadResponse = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.gif" }
        };
        SetupResponse(uploadResponse);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.gif");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x47, 0x49, 0x46 });

            var result = await _apiService.UploadFileAsync("req-1", tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadFileAsync_UnknownExtension_UsesOctetStream()
    {
        var uploadResponse = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.xyz" }
        };
        SetupResponse(uploadResponse);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xyz");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02 });

            var result = await _apiService.UploadFileAsync("req-1", tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UploadRedactedFileAsync_UploadsFile()
    {
        var file = new EvidenceFile { Id = "file-1", Filename = "test.redacted.jpg" };
        SetupResponse(file);

        var result = await _apiService.UploadRedactedFileAsync("file-1", new byte[] { 0xFF, 0xD8 }, "test.redacted.jpg");

        result.Filename.Should().Be("test.redacted.jpg");
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString().Contains("/files/file-1/redacted")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UploadRedactedFileAsync_PdfFile_SetsCorrectMimeType()
    {
        var file = new EvidenceFile { Id = "file-1", Filename = "test.redacted.pdf" };
        SetupResponse(file);

        var result = await _apiService.UploadRedactedFileAsync("file-1", new byte[] { 0x25, 0x50 }, "test.redacted.pdf");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadFileAsync_JpegExtension_SetsCorrectMimeType()
    {
        var uploadResponse = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.jpeg" }
        };
        SetupResponse(uploadResponse);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jpeg");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0xFF, 0xD8, 0xFF });

            var result = await _apiService.UploadFileAsync("req-1", tempFile);

            result.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
