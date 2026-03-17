using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class RequestDetailViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public RequestDetailViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        var request = MockApiService.CreateTestRequest();
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(request);
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EvidenceFile>());
        _services.MockApi.Setup(x => x.GetExportsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Export>());
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.Request.Should().BeNull();
        vm.Files.Should().BeEmpty();
        vm.Exports.Should().BeEmpty();
        vm.Title.Should().BeEmpty();
        vm.Notes.Should().BeEmpty();
        vm.Status.Should().Be("new");
    }

    [Fact]
    public async Task LoadRequestAsync_LoadsRequest()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        await vm.LoadRequestAsync("req-123");

        vm.Request.Should().NotBeNull();
        vm.Title.Should().Be("Test Request");
        vm.Status.Should().Be("new");
    }

    [Fact]
    public async Task LoadRequestAsync_LoadsFiles()
    {
        var files = new List<EvidenceFile> { MockApiService.CreateTestFile() };
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(files);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.Files.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRequestAsync_LoadsExports()
    {
        var exports = new List<Export>
        {
            new Export { Id = "exp-1", Filename = "export.zip" }
        };
        _services.MockApi.Setup(x => x.GetExportsAsync(It.IsAny<string>()))
            .ReturnsAsync(exports);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.Exports.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRequestAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Not found"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ErrorMessage.Should().Contain("Not found");
    }

    [Fact]
    public async Task SaveChangesCommand_UpdatesRequest()
    {
        var updatedRequest = MockApiService.CreateTestRequest();
        updatedRequest.Title = "Updated Title";
        _services.MockApi.Setup(x => x.UpdateRequestAsync(It.IsAny<string>(), It.IsAny<UpdateRequestPayload>()))
            .ReturnsAsync(updatedRequest);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");
        vm.Title = "Updated Title";
        vm.Notes = "Some notes";

        vm.SaveChangesCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateRequestAsync("req-123", It.Is<UpdateRequestPayload>(p =>
            p.Title == "Updated Title" && p.Notes == "Some notes"
        )), Times.Once);
    }

    [Fact]
    public async Task DeleteFileCommand_DeletesFile()
    {
        var file = MockApiService.CreateTestFile();
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EvidenceFile> { file });

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.DeleteFileCommand.Execute(file);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteFileAsync("file-123"), Times.Once);
        vm.Files.Should().BeEmpty();
    }

    [Fact]
    public void OpenFileCommand_RaisesFileSelectedEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var file = MockApiService.CreateTestFile();
        var eventRaised = false;
        EvidenceFile? selectedFile = null;
        vm.FileSelected += (s, f) =>
        {
            eventRaised = true;
            selectedFile = f;
        };

        vm.OpenFileCommand.Execute(file);

        eventRaised.Should().BeTrue();
        selectedFile.Should().Be(file);
        vm.SelectedFile.Should().Be(file);
    }

    [Fact]
    public async Task CreateExportCommand_CreatesExport()
    {
        var export = new Export { Id = "exp-new", Filename = "export.zip" };
        _services.MockApi.Setup(x => x.CreateExportAsync(It.IsAny<string>()))
            .ReturnsAsync(export);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.CreateExportCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.CreateExportAsync("req-123"), Times.Once);
        vm.Exports.Should().Contain(export);
    }

    [Fact]
    public void CloseCommand_RaisesRequestClosedEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var eventRaised = false;
        vm.RequestClosed += (s, e) => eventRaised = true;

        vm.CloseCommand.Execute(null);

        eventRaised.Should().BeTrue();
    }
}
