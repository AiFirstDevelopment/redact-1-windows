using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class RequestDetailViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public RequestDetailViewTests()
    {
        _app = new TestAppBuilder(isAuthenticated: true);
        SetupMocks();
    }

    private void SetupMocks()
    {
        var request = new RecordsRequest
        {
            Id = "req-123",
            RequestNumber = "FOIA-2024-001",
            Title = "Test Request",
            Status = "new",
            Notes = "Test notes"
        };

        var files = new List<EvidenceFile>
        {
            new EvidenceFile { Id = "file-1", Filename = "document.pdf", FileSize = 1024, FileType = "pdf" }
        };

        var exports = new List<Export>();

        _app.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(request);
        _app.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(files);
        _app.MockApi.Setup(x => x.GetExportsAsync(It.IsAny<string>()))
            .ReturnsAsync(exports);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void RequestDetailView_Renders_WithAllElements()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        var textBoxes = view.GetVisualDescendants().OfType<TextBox>().ToList();
        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        // Should have title input, notes input
        textBoxes.Should().HaveCountGreaterThanOrEqualTo(1);
        // Should have save, close, upload buttons
        buttons.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public async Task RequestDetailView_LoadRequest_LoadsData()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        view.LoadRequest("req-123");

        // Wait for async load
        await Task.Delay(200);

        _app.MockApi.Verify(x => x.GetRequestAsync("req-123"), Times.Once);
        _app.MockApi.Verify(x => x.GetFilesAsync("req-123"), Times.Once);

        window.Close();
    }

    [AvaloniaFact]
    public void RequestDetailView_CloseButton_Exists()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var closeButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("close") == true);

        closeButton.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void RequestDetailView_SaveButton_Exists()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var saveButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("save") == true);

        saveButton.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void RequestDetailView_UploadButton_Exists()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var uploadButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("upload") == true);

        uploadButton.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void RequestDetailView_CanSubscribeToFileSelectedEvent()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        EvidenceFile? selectedFile = null;
        view.FileSelected += (s, f) => selectedFile = f;

        // Event subscription should work without throwing
        view.FileSelected -= (s, f) => { };

        window.Close();
    }

    [AvaloniaFact]
    public void RequestDetailView_CanSubscribeToRequestClosedEvent()
    {
        var view = _app.CreateView<RequestDetailView>();
        var window = new Window { Content = view };
        window.Show();

        var closed = false;
        view.RequestClosed += (s, e) => closed = true;

        // Event subscription should work without throwing
        view.RequestClosed -= (s, e) => { };

        window.Close();
    }
}
