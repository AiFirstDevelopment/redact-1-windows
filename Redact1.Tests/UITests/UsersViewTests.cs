using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class UsersViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public UsersViewTests()
    {
        _app = new TestAppBuilder(isAuthenticated: true, isSupervisor: true);
        SetupMocks();
    }

    private void SetupMocks()
    {
        var users = new List<User>
        {
            new User { Id = "user-1", Name = "Admin User", Email = "admin@test.com", Role = "admin" },
            new User { Id = "user-2", Name = "Clerk User", Email = "clerk@test.com", Role = "clerk" }
        };

        _app.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(users);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void UsersView_Renders_WithAllElements()
    {
        var view = _app.CreateView<UsersView>();
        var window = new Window { Content = view };
        window.Show();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        buttons.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void UsersView_Initialize_SetsDataContext()
    {
        var view = _app.CreateView<UsersView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        view.DataContext.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public void UsersView_CreateUserButton_Exists()
    {
        var view = _app.CreateView<UsersView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var createButton = buttons.FirstOrDefault(b =>
            b.Content?.ToString()?.ToLower().Contains("add") == true ||
            b.Content?.ToString()?.ToLower().Contains("create") == true ||
            b.Content?.ToString()?.ToLower().Contains("new") == true ||
            b.Content?.ToString()?.Contains("+") == true);

        createButton.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public async Task UsersView_Initialize_LoadsUsers()
    {
        var view = _app.CreateView<UsersView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        await Task.Delay(200);

        _app.MockApi.Verify(x => x.GetUsersAsync(), Times.AtLeastOnce);

        window.Close();
    }

    [AvaloniaFact]
    public void UsersView_HasUserList()
    {
        var view = _app.CreateView<UsersView>();
        var window = new Window { Content = view };
        window.Show();

        view.Initialize();

        var itemsControls = view.GetVisualDescendants().OfType<ItemsControl>().ToList();
        var listBoxes = view.GetVisualDescendants().OfType<ListBox>().ToList();

        // Should have some list control for users
        (itemsControls.Count + listBoxes.Count).Should().BeGreaterThan(0);

        window.Close();
    }
}
