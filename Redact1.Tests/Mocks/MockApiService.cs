using Moq;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.Tests.Mocks;

public static class MockApiService
{
    public static Mock<IApiService> Create()
    {
        var mock = new Mock<IApiService>();

        // Default successful responses
        mock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new LoginResponse
            {
                Token = "test-token",
                User = CreateTestUser()
            });

        mock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(CreateTestUser());

        mock.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<RecordsRequest>());

        mock.Setup(x => x.GetArchivedRequestsAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<RecordsRequest>());

        mock.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
            .ReturnsAsync((CreateRequestPayload p) => new RecordsRequest
            {
                Id = Guid.NewGuid().ToString(),
                RequestNumber = p.RequestNumber,
                Title = p.Title,
                RequestDate = p.RequestDate,
                Status = "new",
                CreatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            });

        mock.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User> { CreateTestUser() });

        return mock;
    }

    public static User CreateTestUser(bool isSupervisor = false)
    {
        return new User
        {
            Id = "user-123",
            Email = "test@pd.local",
            Name = "Test User",
            Role = isSupervisor ? "supervisor" : "clerk"
        };
    }

    public static RecordsRequest CreateTestRequest()
    {
        return new RecordsRequest
        {
            Id = "req-123",
            RequestNumber = "FOIA-2024-001",
            Title = "Test Request",
            Status = "new",
            RequestDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            CreatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            CreatedBy = "user-123"
        };
    }

    public static EvidenceFile CreateTestFile(bool isPdf = false)
    {
        return new EvidenceFile
        {
            Id = "file-123",
            RequestId = "req-123",
            Filename = isPdf ? "test.pdf" : "test.png",
            FileType = isPdf ? "pdf" : "image",
            MimeType = isPdf ? "application/pdf" : "image/png",
            FileSize = 1024,
            CreatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };
    }

    public static Detection CreateTestDetection()
    {
        return new Detection
        {
            Id = "det-123",
            FileId = "file-123",
            DetectionType = "ssn",
            Status = "pending",
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100,
            BboxHeight = 20,
            Confidence = 0.95
        };
    }
}
