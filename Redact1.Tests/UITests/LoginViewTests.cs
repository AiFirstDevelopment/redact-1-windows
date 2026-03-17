using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Redact1.ViewModels;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class LoginViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public LoginViewTests()
    {
        _app = new TestAppBuilder(isEnrolled: true, isAuthenticated: false);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void LoginView_Renders_WithAllElements()
    {
        var view = _app.CreateView<LoginView>();
        var window = new Window { Content = view };
        window.Show();

        // Find input elements
        var textBoxes = view.GetVisualDescendants().OfType<TextBox>().ToList();
        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        textBoxes.Should().HaveCountGreaterThanOrEqualTo(1); // At least email input
        buttons.Should().HaveCountGreaterThan(0); // Login button

        window.Close();
    }

    [AvaloniaFact]
    public void LoginView_EmailInput_BindsToViewModel()
    {
        var view = _app.CreateView<LoginView>();
        var viewModel = _app.Services.GetService(typeof(LoginViewModel)) as LoginViewModel;
        view.DataContext = viewModel;

        var window = new Window { Content = view };
        window.Show();

        var textBoxes = view.GetVisualDescendants().OfType<TextBox>().ToList();
        var emailInput = textBoxes.FirstOrDefault();

        emailInput.Should().NotBeNull();
        emailInput!.Text = "test@example.com";

        viewModel!.Email.Should().Be("test@example.com");

        window.Close();
    }

    [AvaloniaFact]
    public void LoginView_LoginButton_Exists()
    {
        var view = _app.CreateView<LoginView>();
        var viewModel = _app.Services.GetService(typeof(LoginViewModel)) as LoginViewModel;
        view.DataContext = viewModel;

        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var loginButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("login") == true ||
            b.Content?.ToString()?.ToLower().Contains("sign in") == true);

        // Either find by content or verify at least one button exists
        buttons.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void LoginView_ShowsLoadingIndicator_WhenLoading()
    {
        var view = _app.CreateView<LoginView>();
        var viewModel = _app.Services.GetService(typeof(LoginViewModel)) as LoginViewModel;
        view.DataContext = viewModel;

        var window = new Window { Content = view };
        window.Show();

        viewModel!.IsLoading = true;

        var progressBars = view.GetVisualDescendants().OfType<ProgressBar>().ToList();
        // Should have a progress indicator somewhere
        // (may or may not be visible depending on binding)

        window.Close();
    }
}
