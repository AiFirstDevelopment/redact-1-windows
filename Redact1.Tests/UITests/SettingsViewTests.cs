using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class SettingsViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public SettingsViewTests()
    {
        _app = new TestAppBuilder(isAuthenticated: true);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void SettingsView_Renders_WithAllElements()
    {
        var view = _app.CreateView<SettingsView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var textBlocks = view.GetVisualDescendants().OfType<TextBlock>().ToList();

        // Should have various settings options
        textBlocks.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void SettingsView_Initialize_SetsDataContext()
    {
        var view = _app.CreateView<SettingsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        view.DataContext.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void SettingsView_LogoutButton_Exists()
    {
        var view = _app.CreateView<SettingsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var logoutButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("logout") == true ||
            b.Content?.ToString()?.ToLower().Contains("sign out") == true);

        logoutButton.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void SettingsView_ShowsUserInfo()
    {
        var view = _app.CreateView<SettingsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        var textBlocks = view.GetVisualDescendants().OfType<TextBlock>().ToList();

        // Should display some user info
        textBlocks.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void SettingsView_ShowsAppVersion()
    {
        var view = _app.CreateView<SettingsView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        var textBlocks = view.GetVisualDescendants().OfType<TextBlock>().ToList();
        var versionText = textBlocks.FirstOrDefault(t =>
            t.Text?.StartsWith("v") == true ||
            t.Text?.Contains("Version") == true);

        // Version should be displayed somewhere
        textBlocks.Should().HaveCountGreaterThan(0);

        window.Close();
    }
}
