using FluentAssertions;
using Redact1.Models;

namespace Redact1.Tests.Models;

public class RecordsRequestTests
{
    [Fact]
    public void StatusDisplay_New_ReturnsNew()
    {
        var request = new RecordsRequest { Status = "new" };
        request.StatusDisplay.Should().Be("New");
    }

    [Fact]
    public void StatusDisplay_InProgress_ReturnsInProgress()
    {
        var request = new RecordsRequest { Status = "in_progress" };
        request.StatusDisplay.Should().Be("In Progress");
    }

    [Fact]
    public void StatusDisplay_Completed_ReturnsCompleted()
    {
        var request = new RecordsRequest { Status = "completed" };
        request.StatusDisplay.Should().Be("Completed");
    }

    [Fact]
    public void StatusDisplay_Archived_ReturnsArchived()
    {
        var request = new RecordsRequest { Status = "archived" };
        request.StatusDisplay.Should().Be("Archived");
    }

    [Fact]
    public void StatusDisplay_Unknown_ReturnsRaw()
    {
        var request = new RecordsRequest { Status = "custom_status" };
        request.StatusDisplay.Should().Be("custom_status");
    }

    [Fact]
    public void IsArchived_WithArchivedAt_ReturnsTrue()
    {
        var request = new RecordsRequest { ArchivedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds() };
        request.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void IsArchived_WithoutArchivedAt_ReturnsFalse()
    {
        var request = new RecordsRequest { ArchivedAt = null };
        request.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void RequestDateTime_ConvertsFromUnixTimestamp()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var request = new RecordsRequest { RequestDate = timestamp };

        request.RequestDateTime.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc).ToLocalTime());
    }
}

public class EvidenceFileTests
{
    [Fact]
    public void IsImage_WithImageType_ReturnsTrue()
    {
        var file = new EvidenceFile { FileType = "image" };
        file.IsImage.Should().BeTrue();
    }

    [Fact]
    public void IsImage_WithPdfType_ReturnsFalse()
    {
        var file = new EvidenceFile { FileType = "pdf" };
        file.IsImage.Should().BeFalse();
    }

    [Fact]
    public void IsPdf_WithPdfType_ReturnsTrue()
    {
        var file = new EvidenceFile { FileType = "pdf" };
        file.IsPdf.Should().BeTrue();
    }

    [Fact]
    public void IsPdf_WithImageType_ReturnsFalse()
    {
        var file = new EvidenceFile { FileType = "image" };
        file.IsPdf.Should().BeFalse();
    }

    [Fact]
    public void FileSizeDisplay_Bytes_FormatsCorrectly()
    {
        var file = new EvidenceFile { FileSize = 500 };
        file.FileSizeDisplay.Should().Be("500 B");
    }

    [Fact]
    public void FileSizeDisplay_Kilobytes_FormatsCorrectly()
    {
        var file = new EvidenceFile { FileSize = 2048 };
        file.FileSizeDisplay.Should().Be("2.0 KB");
    }

    [Fact]
    public void FileSizeDisplay_Megabytes_FormatsCorrectly()
    {
        var file = new EvidenceFile { FileSize = 2 * 1024 * 1024 };
        file.FileSizeDisplay.Should().Be("2.0 MB");
    }
}

public class DetectionTests
{
    [Fact]
    public void DisplayName_Face_ReturnsFace()
    {
        var detection = new Detection { DetectionType = "face" };
        detection.DisplayName.Should().Be("Face");
    }

    [Fact]
    public void DisplayName_Plate_ReturnsLicensePlate()
    {
        var detection = new Detection { DetectionType = "plate" };
        detection.DisplayName.Should().Be("License Plate");
    }

    [Fact]
    public void DisplayName_Ssn_ReturnsSSN()
    {
        var detection = new Detection { DetectionType = "ssn" };
        detection.DisplayName.Should().Be("SSN");
    }

    [Fact]
    public void DisplayName_Phone_ReturnsPhoneNumber()
    {
        var detection = new Detection { DetectionType = "phone" };
        detection.DisplayName.Should().Be("Phone Number");
    }

    [Fact]
    public void DisplayName_Email_ReturnsEmailAddress()
    {
        var detection = new Detection { DetectionType = "email" };
        detection.DisplayName.Should().Be("Email Address");
    }

    [Fact]
    public void DisplayName_Address_ReturnsAddress()
    {
        var detection = new Detection { DetectionType = "address" };
        detection.DisplayName.Should().Be("Address");
    }

    [Fact]
    public void DisplayName_Dob_ReturnsDateOfBirth()
    {
        var detection = new Detection { DetectionType = "dob" };
        detection.DisplayName.Should().Be("Date of Birth");
    }

    [Fact]
    public void DisplayName_Unknown_ReturnsRaw()
    {
        var detection = new Detection { DetectionType = "custom_type" };
        detection.DisplayName.Should().Be("custom_type");
    }

    [Fact]
    public void HasBoundingBox_WithAllValues_ReturnsTrue()
    {
        var detection = new Detection
        {
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100,
            BboxHeight = 50
        };
        detection.HasBoundingBox.Should().BeTrue();
    }

    [Fact]
    public void HasBoundingBox_WithMissingValue_ReturnsFalse()
    {
        var detection = new Detection
        {
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100
            // Missing BboxHeight
        };
        detection.HasBoundingBox.Should().BeFalse();
    }

    [Fact]
    public void ConfidenceDisplay_WithValue_FormatsAsPercentage()
    {
        var detection = new Detection { Confidence = 0.95 };
        detection.ConfidenceDisplay.Should().Be("95%");
    }

    [Fact]
    public void ConfidenceDisplay_WithoutValue_ReturnsNA()
    {
        var detection = new Detection { Confidence = null };
        detection.ConfidenceDisplay.Should().Be("N/A");
    }
}

public class UserTests
{
    [Fact]
    public void IsSupervisor_WithSupervisorRole_ReturnsTrue()
    {
        var user = new User { Role = "supervisor" };
        user.IsSupervisor.Should().BeTrue();
    }

    [Fact]
    public void IsSupervisor_WithAdminRole_ReturnsTrue()
    {
        var user = new User { Role = "admin" };
        user.IsSupervisor.Should().BeTrue();
    }

    [Fact]
    public void IsSupervisor_WithClerkRole_ReturnsFalse()
    {
        var user = new User { Role = "clerk" };
        user.IsSupervisor.Should().BeFalse();
    }

    [Fact]
    public void RoleDisplay_Clerk_ReturnsClerk()
    {
        var user = new User { Role = "clerk" };
        user.RoleDisplay.Should().Be("Clerk");
    }

    [Fact]
    public void RoleDisplay_Supervisor_ReturnsSupervisor()
    {
        var user = new User { Role = "supervisor" };
        user.RoleDisplay.Should().Be("Supervisor");
    }

    [Fact]
    public void RoleDisplay_Admin_ReturnsAdministrator()
    {
        var user = new User { Role = "admin" };
        user.RoleDisplay.Should().Be("Administrator");
    }

    [Fact]
    public void RoleDisplay_Unknown_ReturnsRaw()
    {
        var user = new User { Role = "custom_role" };
        user.RoleDisplay.Should().Be("custom_role");
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var user = new User();

        user.Id.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.Name.Should().BeEmpty();
        user.Role.Should().Be("clerk");
        user.EmployeeId.Should().BeNull();
    }
}

public class ExportTests
{
    [Fact]
    public void CreatedDateTime_ConvertsFromUnixTimestamp()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var export = new Export { CreatedAt = timestamp };

        export.CreatedDateTime.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc).ToLocalTime());
    }
}

public class AuditLogTests
{
    [Fact]
    public void ActionDisplay_Create_ReturnsCreated()
    {
        var log = new AuditLog { Action = "create" };
        log.ActionDisplay.Should().Be("Created");
    }

    [Fact]
    public void ActionDisplay_Update_ReturnsUpdated()
    {
        var log = new AuditLog { Action = "update" };
        log.ActionDisplay.Should().Be("Updated");
    }

    [Fact]
    public void ActionDisplay_Delete_ReturnsDeleted()
    {
        var log = new AuditLog { Action = "delete" };
        log.ActionDisplay.Should().Be("Deleted");
    }

    [Fact]
    public void ActionDisplay_Upload_ReturnsUploaded()
    {
        var log = new AuditLog { Action = "upload" };
        log.ActionDisplay.Should().Be("Uploaded");
    }

    [Fact]
    public void ActionDisplay_Download_ReturnsDownloaded()
    {
        var log = new AuditLog { Action = "download" };
        log.ActionDisplay.Should().Be("Downloaded");
    }

    [Fact]
    public void ActionDisplay_Export_ReturnsExported()
    {
        var log = new AuditLog { Action = "export" };
        log.ActionDisplay.Should().Be("Exported");
    }

    [Fact]
    public void ActionDisplay_Approve_ReturnsApproved()
    {
        var log = new AuditLog { Action = "approve" };
        log.ActionDisplay.Should().Be("Approved");
    }

    [Fact]
    public void ActionDisplay_Reject_ReturnsRejected()
    {
        var log = new AuditLog { Action = "reject" };
        log.ActionDisplay.Should().Be("Rejected");
    }

    [Fact]
    public void ActionDisplay_Archive_ReturnsArchived()
    {
        var log = new AuditLog { Action = "archive" };
        log.ActionDisplay.Should().Be("Archived");
    }

    [Fact]
    public void ActionDisplay_Unarchive_ReturnsUnarchived()
    {
        var log = new AuditLog { Action = "unarchive" };
        log.ActionDisplay.Should().Be("Unarchived");
    }

    [Fact]
    public void ActionDisplay_Unknown_ReturnsRaw()
    {
        var log = new AuditLog { Action = "custom_action" };
        log.ActionDisplay.Should().Be("custom_action");
    }

    [Fact]
    public void CreatedAt_ConvertsFromUnixTimestamp()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var log = new AuditLog { CreatedAt = timestamp };

        log.CreatedDateTime.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc).ToLocalTime());
    }
}

public class AgencyTests
{
    [Fact]
    public void SupportsEmail_WithEmailIdentifier_ReturnsTrue()
    {
        var agency = new Agency { LoginIdentifiers = "email" };
        agency.SupportsEmail.Should().BeTrue();
    }

    [Fact]
    public void SupportsEmail_WithMultipleIdentifiers_ReturnsTrue()
    {
        var agency = new Agency { LoginIdentifiers = "email,employeeId" };
        agency.SupportsEmail.Should().BeTrue();
    }

    [Fact]
    public void SupportsEmail_WithoutEmailIdentifier_ReturnsFalse()
    {
        var agency = new Agency { LoginIdentifiers = "employeeId" };
        agency.SupportsEmail.Should().BeFalse();
    }

    [Fact]
    public void SupportsEmployeeId_WithEmployeeIdIdentifier_ReturnsTrue()
    {
        var agency = new Agency { LoginIdentifiers = "employeeId" };
        agency.SupportsEmployeeId.Should().BeTrue();
    }

    [Fact]
    public void SupportsEmployeeId_WithMultipleIdentifiers_ReturnsTrue()
    {
        var agency = new Agency { LoginIdentifiers = "email,employeeId" };
        agency.SupportsEmployeeId.Should().BeTrue();
    }

    [Fact]
    public void SupportsEmployeeId_WithoutEmployeeIdIdentifier_ReturnsFalse()
    {
        var agency = new Agency { LoginIdentifiers = "email" };
        agency.SupportsEmployeeId.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var agency = new Agency();

        agency.Id.Should().BeEmpty();
        agency.Code.Should().BeEmpty();
        agency.Name.Should().BeEmpty();
        agency.LoginIdentifiers.Should().Be("email");
    }
}

public class AgencyConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new AgencyConfig();

        config.Code.Should().BeEmpty();
        config.Name.Should().BeEmpty();
        config.ApiBaseUrl.Should().BeEmpty();
        config.LoginIdentifiers.Should().ContainSingle();
        config.LoginIdentifiers[0].Should().Be("email");
    }
}

public class ManualRedactionTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var redaction = new ManualRedaction();

        redaction.Id.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var redaction = new ManualRedaction
        {
            Id = "red-1",
            FileId = "file-1",
            BboxX = 0.1,
            BboxY = 0.2,
            BboxWidth = 0.3,
            BboxHeight = 0.4,
            PageNumber = 2
        };

        redaction.Id.Should().Be("red-1");
        redaction.FileId.Should().Be("file-1");
        redaction.BboxX.Should().Be(0.1);
        redaction.BboxY.Should().Be(0.2);
        redaction.BboxWidth.Should().Be(0.3);
        redaction.BboxHeight.Should().Be(0.4);
        redaction.PageNumber.Should().Be(2);
    }
}

public class CreateDetectionRequestTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var request = new CreateDetectionRequest
        {
            DetectionType = "face",
            BboxX = 0.1,
            BboxY = 0.2,
            BboxWidth = 0.3,
            BboxHeight = 0.4,
            PageNumber = 1,
            TextContent = "test",
            TextStart = 0,
            TextEnd = 4,
            Confidence = 0.95
        };

        request.DetectionType.Should().Be("face");
        request.BboxX.Should().Be(0.1);
        request.BboxY.Should().Be(0.2);
        request.BboxWidth.Should().Be(0.3);
        request.BboxHeight.Should().Be(0.4);
        request.PageNumber.Should().Be(1);
        request.TextContent.Should().Be("test");
        request.TextStart.Should().Be(0);
        request.TextEnd.Should().Be(4);
        request.Confidence.Should().Be(0.95);
    }
}

public class CreateManualRedactionRequestTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var request = new CreateManualRedactionRequest
        {
            BboxX = 0.1,
            BboxY = 0.2,
            BboxWidth = 0.3,
            BboxHeight = 0.4,
            PageNumber = 1
        };

        request.BboxX.Should().Be(0.1);
        request.BboxY.Should().Be(0.2);
        request.BboxWidth.Should().Be(0.3);
        request.BboxHeight.Should().Be(0.4);
        request.PageNumber.Should().Be(1);
    }
}

public class CreateRequestPayloadTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var payload = new CreateRequestPayload
        {
            Title = "Test Request",
            RequestNumber = "FOIA-001",
            RequestDate = 1234567890,
            Notes = "Test notes"
        };

        payload.Title.Should().Be("Test Request");
        payload.RequestNumber.Should().Be("FOIA-001");
        payload.RequestDate.Should().Be(1234567890);
        payload.Notes.Should().Be("Test notes");
    }
}

public class UpdateRequestPayloadTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var payload = new UpdateRequestPayload
        {
            Title = "Updated Title",
            Status = "in_progress",
            Notes = "Updated notes"
        };

        payload.Title.Should().Be("Updated Title");
        payload.Status.Should().Be("in_progress");
        payload.Notes.Should().Be("Updated notes");
    }
}

public class LoginRequestTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var request = new LoginRequest
        {
            Email = "test@test.com",
            EmployeeId = "EMP001",
            Password = "password123"
        };

        request.Email.Should().Be("test@test.com");
        request.EmployeeId.Should().Be("EMP001");
        request.Password.Should().Be("password123");
    }
}

public class LoginResponseTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var response = new LoginResponse
        {
            Token = "jwt-token",
            User = new User { Id = "user-1", Name = "Test User" }
        };

        response.Token.Should().Be("jwt-token");
        response.User.Id.Should().Be("user-1");
        response.User.Name.Should().Be("Test User");
    }
}

public class CreateUserRequestTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var request = new CreateUserRequest
        {
            Name = "Test User",
            Email = "test@test.com",
            Role = "clerk",
            Password = "password123"
        };

        request.Name.Should().Be("Test User");
        request.Email.Should().Be("test@test.com");
        request.Role.Should().Be("clerk");
        request.Password.Should().Be("password123");
    }
}

public class UpdateUserRequestTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var request = new UpdateUserRequest
        {
            Name = "Updated Name",
            Role = "supervisor",
            Email = "updated@test.com",
            Password = "newpassword"
        };

        request.Name.Should().Be("Updated Name");
        request.Role.Should().Be("supervisor");
        request.Email.Should().Be("updated@test.com");
        request.Password.Should().Be("newpassword");
    }
}

public class UpdateDetectionRequestTests
{
    [Fact]
    public void Status_CanBeSet()
    {
        var request = new UpdateDetectionRequest
        {
            Status = "approved"
        };

        request.Status.Should().Be("approved");
    }

    [Fact]
    public void BboxProperties_CanBeSet()
    {
        var request = new UpdateDetectionRequest
        {
            BboxX = 0.1,
            BboxY = 0.2,
            BboxWidth = 0.3,
            BboxHeight = 0.4
        };

        request.BboxX.Should().Be(0.1);
        request.BboxY.Should().Be(0.2);
        request.BboxWidth.Should().Be(0.3);
        request.BboxHeight.Should().Be(0.4);
    }

    [Fact]
    public void BboxProperties_AreNullable()
    {
        var request = new UpdateDetectionRequest();

        request.BboxX.Should().BeNull();
        request.BboxY.Should().BeNull();
        request.BboxWidth.Should().BeNull();
        request.BboxHeight.Should().BeNull();
    }

    [Fact]
    public void Status_IsNullable()
    {
        var request = new UpdateDetectionRequest();

        request.Status.Should().BeNull();
    }
}

public class UpdateManualRedactionRequestTests
{
    [Fact]
    public void BboxProperties_CanBeSet()
    {
        var request = new UpdateManualRedactionRequest
        {
            BboxX = 0.1,
            BboxY = 0.2,
            BboxWidth = 0.3,
            BboxHeight = 0.4
        };

        request.BboxX.Should().Be(0.1);
        request.BboxY.Should().Be(0.2);
        request.BboxWidth.Should().Be(0.3);
        request.BboxHeight.Should().Be(0.4);
    }

    [Fact]
    public void BboxProperties_AreNullable()
    {
        var request = new UpdateManualRedactionRequest();

        request.BboxX.Should().BeNull();
        request.BboxY.Should().BeNull();
        request.BboxWidth.Should().BeNull();
        request.BboxHeight.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_AreNull()
    {
        var request = new UpdateManualRedactionRequest();

        request.BboxX.Should().BeNull();
        request.BboxY.Should().BeNull();
        request.BboxWidth.Should().BeNull();
        request.BboxHeight.Should().BeNull();
    }
}

public class FileUploadResponseTests
{
    [Fact]
    public void Properties_CanBeSet()
    {
        var response = new FileUploadResponse
        {
            File = new EvidenceFile { Id = "file-1", Filename = "test.pdf" }
        };

        response.File.Id.Should().Be("file-1");
        response.File.Filename.Should().Be("test.pdf");
    }
}

public class DetectedRegionTests
{
    [Fact]
    public void Properties_HaveDefaultValues()
    {
        var region = new Redact1.Services.DetectedRegion();

        region.Type.Should().BeEmpty();
        region.X.Should().Be(0);
        region.Y.Should().Be(0);
        region.Width.Should().Be(0);
        region.Height.Should().Be(0);
        region.Confidence.Should().Be(0);
        region.TextContent.Should().BeNull();
        region.TextStart.Should().BeNull();
        region.TextEnd.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var region = new Redact1.Services.DetectedRegion
        {
            Type = "face",
            X = 10.5,
            Y = 20.5,
            Width = 100.5,
            Height = 50.5,
            Confidence = 0.95,
            TextContent = "test content",
            TextStart = 0,
            TextEnd = 12
        };

        region.Type.Should().Be("face");
        region.X.Should().Be(10.5);
        region.Y.Should().Be(20.5);
        region.Width.Should().Be(100.5);
        region.Height.Should().Be(50.5);
        region.Confidence.Should().Be(0.95);
        region.TextContent.Should().Be("test content");
        region.TextStart.Should().Be(0);
        region.TextEnd.Should().Be(12);
    }
}
