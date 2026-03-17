using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class MainViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public MainViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesWithCurrentUser()
    {
        var vm = _services.GetService<MainViewModel>();

        vm.CurrentUser.Should().NotBeNull();
        vm.CurrentUser!.Email.Should().Be("test@pd.local");
    }

    [Fact]
    public void IsSupervisor_ReturnsFalse_WhenNotSupervisor()
    {
        var vm = _services.GetService<MainViewModel>();

        vm.IsSupervisor.Should().BeFalse();
    }

    [Fact]
    public void IsSupervisor_ReturnsTrue_WhenSupervisor()
    {
        using var services = new TestServiceProvider(isAuthenticated: true, isSupervisor: true);
        services.SetupApp();
        var vm = services.GetService<MainViewModel>();

        vm.IsSupervisor.Should().BeTrue();
    }

    [Fact]
    public void SelectedTabIndex_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<MainViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.SelectedTabIndex))
                propertyChanged = true;
        };

        vm.SelectedTabIndex = 2;

        propertyChanged.Should().BeTrue();
        vm.SelectedTabIndex.Should().Be(2);
    }

    [Fact]
    public async Task LogoutCommand_CallsLogoutAsync()
    {
        var vm = _services.GetService<MainViewModel>();

        vm.LogoutCommand.Execute(null);
        await Task.Delay(100);

        _services.MockAuth.Verify(x => x.LogoutAsync(), Times.Once);
    }

    [Fact]
    public void AuthStateChanged_UpdatesCurrentUser()
    {
        var vm = _services.GetService<MainViewModel>();
        var newUser = new User { Id = "new-user", Email = "new@pd.local", Name = "New User" };

        _services.MockAuth.SetupGet(x => x.CurrentUser).Returns(newUser);
        _services.MockAuth.Raise(x => x.AuthStateChanged += null, _services.MockAuth.Object, newUser);

        vm.CurrentUser.Should().Be(newUser);
    }

    [Fact]
    public void AuthStateChanged_WhenLoggedOut_RaisesLoggedOutEvent()
    {
        var vm = _services.GetService<MainViewModel>();
        var eventRaised = false;
        vm.LoggedOut += (s, e) => eventRaised = true;

        _services.MockAuth.SetupGet(x => x.CurrentUser).Returns((User?)null);
        _services.MockAuth.Raise(x => x.AuthStateChanged += null, _services.MockAuth.Object, null);

        eventRaised.Should().BeTrue();
    }
}
