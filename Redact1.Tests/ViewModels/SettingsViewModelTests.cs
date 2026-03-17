using FluentAssertions;
using Moq;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class SettingsViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public SettingsViewModelTests()
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
        var vm = _services.GetService<SettingsViewModel>();

        vm.CurrentUser.Should().NotBeNull();
        vm.CurrentUser!.Email.Should().Be("test@pd.local");
    }

    [Fact]
    public void Constructor_SetsAppVersion()
    {
        var vm = _services.GetService<SettingsViewModel>();

        vm.AppVersion.Should().StartWith("v");
    }

    [Fact]
    public async Task LogoutCommand_CallsLogoutAsync()
    {
        var vm = _services.GetService<SettingsViewModel>();
        var eventRaised = false;
        vm.LoggedOut += (s, e) => eventRaised = true;

        vm.LogoutCommand.Execute(null);
        await Task.Delay(100);

        _services.MockAuth.Verify(x => x.LogoutAsync(), Times.Once);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void OpenSupportCommand_DoesNotThrow()
    {
        var vm = _services.GetService<SettingsViewModel>();

        var exception = Record.Exception(() => vm.OpenSupportCommand.Execute(null));

        exception.Should().BeNull();
    }

    [Fact]
    public void OpenAboutCommand_DoesNotThrow()
    {
        var vm = _services.GetService<SettingsViewModel>();

        var exception = Record.Exception(() => vm.OpenAboutCommand.Execute(null));

        exception.Should().BeNull();
    }
}
