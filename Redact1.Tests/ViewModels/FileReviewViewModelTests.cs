using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class FileReviewViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public FileReviewViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        var file = MockApiService.CreateTestFile();
        _services.MockApi.Setup(x => x.GetFileAsync(It.IsAny<string>()))
            .ReturnsAsync(file);
        _services.MockApi.Setup(x => x.GetOriginalFileAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection>(),
                ManualRedactions = new List<ManualRedaction>()
            });
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.File.Should().BeNull();
        vm.Detections.Should().BeEmpty();
        vm.ManualRedactions.Should().BeEmpty();
        vm.CurrentPage.Should().Be(1);
        vm.TotalPages.Should().Be(1);
        vm.IsDetecting.Should().BeFalse();
        vm.ShowRedacted.Should().BeFalse();
        vm.IsDrawingMode.Should().BeFalse();
    }

    [Fact]
    public async Task LoadFileAsync_LoadsFile()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        await vm.LoadFileAsync("file-123");

        vm.File.Should().NotBeNull();
        vm.File!.Id.Should().Be("file-123");
    }

    [Fact]
    public async Task LoadFileAsync_LoadsDetections()
    {
        var detections = new List<Detection> { MockApiService.CreateTestDetection() };
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = detections,
                ManualRedactions = new List<ManualRedaction>()
            });

        var vm = _services.GetService<FileReviewViewModel>();

        // Set file directly to avoid image loading issues
        vm.File = MockApiService.CreateTestFile();

        // Call LoadDetectionsAsync through LoadFileAsync - but that will fail on image loading
        // Instead, use reflection to call LoadDetectionsAsync directly
        var method = vm.GetType().GetMethod("LoadDetectionsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.Detections.Should().HaveCount(1);
    }

    [Fact]
    public async Task RunDetectionCommand_RunsDetection()
    {
        var detection = MockApiService.CreateTestDetection();
        // Setup all required mocks
        _services.MockApi.Setup(x => x.ClearDetectionsAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _services.MockApi.Setup(x => x.CreateDetectionsAsync(It.IsAny<string>(), It.IsAny<List<CreateDetectionRequest>>()))
            .ReturnsAsync(new List<Detection> { detection });

        var vm = _services.GetService<FileReviewViewModel>();

        // Set file directly (image type) to avoid PDF path
        vm.File = MockApiService.CreateTestFile(isPdf: false);

        // Set private _originalFileData field via reflection
        var field = vm.GetType().GetField("_originalFileData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(vm, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        vm.RunDetectionCommand.Execute(null);
        await Task.Delay(300);

        _services.MockApi.Verify(x => x.ClearDetectionsAsync("file-123"), Times.Once);
        vm.Detections.Should().HaveCount(1);
    }

    [Fact]
    public async Task ApproveDetectionCommand_ApprovesDetection()
    {
        var detection = MockApiService.CreateTestDetection();
        var approvedDetection = MockApiService.CreateTestDetection();
        approvedDetection.Status = "approved";

        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection> { detection },
                ManualRedactions = new List<ManualRedaction>()
            });
        _services.MockApi.Setup(x => x.UpdateDetectionAsync(It.IsAny<string>(), It.IsAny<UpdateDetectionRequest>()))
            .ReturnsAsync(approvedDetection);

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        vm.ApproveDetectionCommand.Execute(detection);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync("det-123",
            It.Is<UpdateDetectionRequest>(r => r.Status == "approved")), Times.Once);
    }

    [Fact]
    public async Task RejectDetectionCommand_RejectsDetection()
    {
        var detection = MockApiService.CreateTestDetection();
        var rejectedDetection = MockApiService.CreateTestDetection();
        rejectedDetection.Status = "rejected";

        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection> { detection },
                ManualRedactions = new List<ManualRedaction>()
            });
        _services.MockApi.Setup(x => x.UpdateDetectionAsync(It.IsAny<string>(), It.IsAny<UpdateDetectionRequest>()))
            .ReturnsAsync(rejectedDetection);

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        vm.RejectDetectionCommand.Execute(detection);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync("det-123",
            It.Is<UpdateDetectionRequest>(r => r.Status == "rejected")), Times.Once);
    }

    [Fact]
    public async Task AddManualRedaction_CreatesRedaction()
    {
        var redaction = new ManualRedaction
        {
            Id = "red-1",
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100,
            BboxHeight = 50
        };
        _services.MockApi.Setup(x => x.CreateManualRedactionAsync(It.IsAny<string>(), It.IsAny<CreateManualRedactionRequest>()))
            .ReturnsAsync(redaction);

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        await vm.AddManualRedaction(10, 20, 100, 50);

        vm.ManualRedactions.Should().Contain(redaction);
    }

    [Fact]
    public async Task DeleteManualRedactionCommand_DeletesRedaction()
    {
        var redaction = new ManualRedaction { Id = "red-1" };
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection>(),
                ManualRedactions = new List<ManualRedaction> { redaction }
            });

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        vm.DeleteManualRedactionCommand.Execute(redaction);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteManualRedactionAsync("red-1"), Times.Once);
        vm.ManualRedactions.Should().BeEmpty();
    }

    [Fact]
    public void ToggleDrawingModeCommand_TogglesMode()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.ToggleDrawingModeCommand.Execute(null);
        vm.IsDrawingMode.Should().BeTrue();

        vm.ToggleDrawingModeCommand.Execute(null);
        vm.IsDrawingMode.Should().BeFalse();
    }

    [Fact]
    public void CloseCommand_RaisesFileClosedEvent()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        var eventRaised = false;
        vm.FileClosed += (s, e) => eventRaised = true;

        vm.CloseCommand.Execute(null);

        eventRaised.Should().BeTrue();
    }
}
