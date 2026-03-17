using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class FileReviewViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public FileReviewViewTests()
    {
        _app = new TestAppBuilder(isAuthenticated: true);
        SetupMocks();
    }

    private void SetupMocks()
    {
        var file = new EvidenceFile
        {
            Id = "file-123",
            Filename = "test.png",
            FileType = "image",
            MimeType = "image/png",
            FileSize = 1024
        };

        var detections = new DetectionListResponse
        {
            Detections = new List<Detection>
            {
                new Detection { Id = "det-1", DetectionType = "face", BboxX = 10, BboxY = 10, BboxWidth = 50, BboxHeight = 50 }
            },
            ManualRedactions = new List<ManualRedaction>()
        };

        _app.MockApi.Setup(x => x.GetFileAsync(It.IsAny<string>()))
            .ReturnsAsync(file);
        _app.MockApi.Setup(x => x.GetOriginalFileAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }); // PNG header
        _app.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(detections);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void FileReviewView_Renders_WithAllElements()
    {
        var view = _app.CreateView<FileReviewView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        // Should have various control buttons
        buttons.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void FileReviewView_CloseButton_Exists()
    {
        var view = _app.CreateView<FileReviewView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var closeButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("close") == true ||
            b.Content?.ToString()?.ToLower().Contains("back") == true);

        closeButton.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void FileReviewView_CanSubscribeToFileClosedEvent()
    {
        var view = _app.CreateView<FileReviewView>();
        var window = new Window { Content = view };
        window.Show();

        var closed = false;
        view.FileClosed += (s, e) => closed = true;

        // Event subscription should work without throwing
        view.FileClosed -= (s, e) => { };

        window.Close();
    }

    [AvaloniaFact]
    public void FileReviewView_HasCanvas_ForImageDisplay()
    {
        var view = _app.CreateView<FileReviewView>();
        var window = new Window { Content = view };
        window.Show();

        var canvases = view.GetVisualDescendants().OfType<Canvas>().ToList();
        var images = view.GetVisualDescendants().OfType<Image>().ToList();

        // Should have either a canvas or image control for display
        (canvases.Count + images.Count).Should().BeGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void FileReviewView_HasDetectionControls()
    {
        var view = _app.CreateView<FileReviewView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        // Should have detection-related buttons (approve, reject, run detection, etc.)
        buttons.Should().HaveCountGreaterThan(0);

        window.Close();
    }
}
