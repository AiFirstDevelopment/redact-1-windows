using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using Redact1.ViewModels;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class EnrollmentViewTests : IDisposable
{
    private readonly TestAppBuilder _app;

    public EnrollmentViewTests()
    {
        _app = new TestAppBuilder(isEnrolled: false);
    }

    public void Dispose() => _app.Dispose();

    [AvaloniaFact]
    public void EnrollmentView_Renders_WithAllElements()
    {
        var view = _app.CreateView<EnrollmentView>();
        var window = new Window { Content = view };
        window.Show();

        // Find elements
        var codeInput = view.FindDescendantOfType<TextBox>();
        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();

        codeInput.Should().NotBeNull();
        buttons.Should().HaveCountGreaterThan(0);

        window.Close();
    }

    [AvaloniaFact]
    public void EnrollmentView_CodeInput_BindsToViewModel()
    {
        var view = _app.CreateView<EnrollmentView>();
        var viewModel = new EnrollmentViewModel();
        view.DataContext = viewModel;

        var window = new Window { Content = view };
        window.Show();

        var codeInput = view.FindDescendantOfType<TextBox>();
        codeInput.Should().NotBeNull();

        // Type into the input
        codeInput!.Focus();
        codeInput.Text = "TEST-CODE";

        viewModel.DepartmentCode.Should().Be("TEST-CODE");

        window.Close();
    }

    [AvaloniaFact]
    public void EnrollmentView_DemoButton_SetsDemoCode()
    {
        var view = _app.CreateView<EnrollmentView>();
        var viewModel = new EnrollmentViewModel();
        view.DataContext = viewModel;

        var window = new Window { Content = view };
        window.Show();

        // Find demo button and click it
        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        var demoButton = buttons.FirstOrDefault(b => b.Content?.ToString()?.Contains("Demo") == true);

        if (demoButton != null)
        {
            demoButton.Command?.Execute(null);
            viewModel.DepartmentCode.Should().Be("DEMO");
        }

        window.Close();
    }
}
