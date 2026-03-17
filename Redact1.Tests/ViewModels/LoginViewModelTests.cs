using FluentAssertions;
using Moq;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class LoginViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public LoginViewModelTests()
    {
        _services = new TestServiceProvider();
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<LoginViewModel>();

        vm.Email.Should().BeEmpty();
        vm.Password.Should().BeEmpty();
        vm.IsLoading.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
        vm.LoginCommand.Should().NotBeNull();
        vm.UseDemoCredentialsCommand.Should().NotBeNull();
    }

    [Fact]
    public void UseDemoCredentialsCommand_SetsCredentials()
    {
        var vm = _services.GetService<LoginViewModel>();

        vm.UseDemoCredentialsCommand.Execute(null);

        vm.Email.Should().Be("supervisor@test.com");
        vm.Password.Should().Be("test123");
    }

    [Fact]
    public void LoginCommand_WithEmptyEmail_SetsError()
    {
        var vm = _services.GetService<LoginViewModel>();
        vm.Email = "";
        vm.Password = "password";

        vm.LoginCommand.Execute(null);

        vm.ErrorMessage.Should().Contain("email and password");
    }

    [Fact]
    public void LoginCommand_WithEmptyPassword_SetsError()
    {
        var vm = _services.GetService<LoginViewModel>();
        vm.Email = "test@test.com";
        vm.Password = "";

        vm.LoginCommand.Execute(null);

        vm.ErrorMessage.Should().Contain("email and password");
    }

    [Fact]
    public async Task LoginCommand_WithValidCredentials_CallsLoginAsync()
    {
        var vm = _services.GetService<LoginViewModel>();
        vm.Email = "test@pd.local";
        vm.Password = "test-password";
        var eventRaised = false;
        vm.LoginSucceeded += (s, e) => eventRaised = true;

        vm.LoginCommand.Execute(null);
        await Task.Delay(100); // Wait for async

        _services.MockAuth.Verify(x => x.LoginAsync("test@pd.local", "test-password", false), Times.Once);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task LoginCommand_WhenLoginFails_SetsError()
    {
        _services.MockAuth.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Invalid credentials"));

        var vm = _services.GetService<LoginViewModel>();
        vm.Email = "test@pd.local";
        vm.Password = "wrong-password";

        vm.LoginCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Invalid credentials");
    }

    [Fact]
    public void Email_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<LoginViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Email))
                propertyChanged = true;
        };

        vm.Email = "new@email.com";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void Password_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<LoginViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Password))
                propertyChanged = true;
        };

        vm.Password = "new-password";

        propertyChanged.Should().BeTrue();
    }
}
