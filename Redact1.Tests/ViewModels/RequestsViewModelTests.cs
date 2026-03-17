using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class RequestsViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public RequestsViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<RequestsViewModel>();

        vm.Requests.Should().BeEmpty();
        vm.SearchText.Should().BeEmpty();
        vm.StatusFilter.Should().Be("all");
        vm.ShowArchived.Should().BeFalse();
        vm.CreateRequestCommand.Should().NotBeNull();
        vm.OpenRequestCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadRequestsAsync_LoadsRequests()
    {
        var requests = new List<RecordsRequest>
        {
            MockApiService.CreateTestRequest(),
            MockApiService.CreateTestRequest()
        };
        _services.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(requests);

        var vm = _services.GetService<RequestsViewModel>();
        await vm.LoadRequestsAsync();

        vm.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadRequestsAsync_ShowArchived_LoadsArchivedRequests()
    {
        var requests = new List<RecordsRequest> { MockApiService.CreateTestRequest() };
        _services.MockApi.Setup(x => x.GetArchivedRequestsAsync(It.IsAny<string?>()))
            .ReturnsAsync(requests);

        var vm = _services.GetService<RequestsViewModel>();
        vm.ShowArchived = true;
        await vm.LoadRequestsAsync();

        _services.MockApi.Verify(x => x.GetArchivedRequestsAsync(null), Times.Once);
        vm.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRequestsAsync_WithStatusFilter_FiltersRequests()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.StatusFilter = "in_progress";

        // Wait for the debounced load
        await Task.Delay(200);

        _services.MockApi.Verify(x => x.GetRequestsAsync("in_progress", null), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadRequestsAsync_WithSearchText_SearchesRequests()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.SearchText = "FOIA-2024";

        await Task.Delay(200);

        _services.MockApi.Verify(x => x.GetRequestsAsync(null, "FOIA-2024"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadRequestsAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Network error"));

        var vm = _services.GetService<RequestsViewModel>();
        await vm.LoadRequestsAsync();

        vm.ErrorMessage.Should().Contain("Network error");
    }

    [Fact]
    public async Task CreateRequestCommand_CreatesRequest()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var eventRaised = false;
        RecordsRequest? selectedRequest = null;
        vm.RequestSelected += (s, r) =>
        {
            eventRaised = true;
            selectedRequest = r;
        };

        vm.CreateRequestCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()), Times.Once);
        vm.Requests.Should().HaveCount(1);
        eventRaised.Should().BeTrue();
        selectedRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRequestCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()))
            .ThrowsAsync(new Exception("Failed to create"));

        var vm = _services.GetService<RequestsViewModel>();
        vm.CreateRequestCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Failed to create");
    }

    [Fact]
    public void OpenRequestCommand_RaisesRequestSelectedEvent()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        var eventRaised = false;
        RecordsRequest? selectedRequest = null;
        vm.RequestSelected += (s, r) =>
        {
            eventRaised = true;
            selectedRequest = r;
        };

        vm.OpenRequestCommand.Execute(request);

        eventRaised.Should().BeTrue();
        selectedRequest.Should().Be(request);
    }

    [Fact]
    public void OpenRequestCommand_WithNull_DoesNotRaiseEvent()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var eventRaised = false;
        vm.RequestSelected += (s, r) => eventRaised = true;

        vm.OpenRequestCommand.Execute(null);

        eventRaised.Should().BeFalse();
    }
}
