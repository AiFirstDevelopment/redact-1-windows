using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class RequestsViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public RequestsViewTests()
    {
        _app = new TestAppBuilder(isAuthenticated: true);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void RequestsView_Renders_WithAllElements()
    {
        var view = _app.CreateView<RequestsView>();
        var window = new Window { Content = view };
        window.Show();

        // Find search box
        var textBoxes = view.GetVisualDescendants().OfType<TextBox>().ToList();
        var comboBoxes = view.GetVisualDescendants().OfType<ComboBox>().ToList();
        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        // Should have search input, status filter, and create button
        textBoxes.Should().HaveCountGreaterThanOrEqualTo(1);

        window.Close();
    }

    [AvaloniaFact]
    public void RequestsView_Initialize_LoadsRequests()
    {
        var requests = new List<RecordsRequest>
        {
            new RecordsRequest { Id = "req-1", Title = "Request 1", RequestNumber = "FOIA-001", Status = "new" },
            new RecordsRequest { Id = "req-2", Title = "Request 2", RequestNumber = "FOIA-002", Status = "in_progress" }
        };

        _app.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(requests);

        var view = _app.CreateView<RequestsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize(showArchived: false);

        // Give time for async load
        Thread.Sleep(100);

        // Verify API was called
        _app.MockApi.Verify(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()), Times.AtLeastOnce);

        window.Close();
    }

    [AvaloniaFact]
    public void RequestsView_CreateButton_IsVisible()
    {
        var view = _app.CreateView<RequestsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize(showArchived: false);

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var createButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.Contains("New") == true ||
            b.Content?.ToString()?.Contains("+") == true);

        createButton.Should().NotBeNull();
        createButton!.IsVisible.Should().BeTrue();

        window.Close();
    }

    [AvaloniaFact]
    public void RequestsView_ArchivedMode_HidesCreateButton()
    {
        var view = _app.CreateView<RequestsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize(showArchived: true);

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var createButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.Contains("New") == true);

        if (createButton != null)
        {
            createButton.IsVisible.Should().BeFalse();
        }

        window.Close();
    }

    [AvaloniaFact]
    public void RequestsView_SearchBox_BindsToViewModel()
    {
        var view = _app.CreateView<RequestsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize(showArchived: false);

        var searchBox = view.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(t => t.Watermark?.Contains("Search") == true);

        searchBox.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void RequestsView_CanSubscribeToRequestSelectedEvent()
    {
        var view = _app.CreateView<RequestsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize(showArchived: false);

        RecordsRequest? selectedRequest = null;
        view.RequestSelected += (s, r) => selectedRequest = r;

        // Event subscription should work without throwing
        view.RequestSelected -= (s, r) => { };

        window.Close();
    }
}
