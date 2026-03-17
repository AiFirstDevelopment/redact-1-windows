using FluentAssertions;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class EnrollmentViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public EnrollmentViewModelTests()
    {
        _services = new TestServiceProvider(isEnrolled: false);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<EnrollmentViewModel>();

        vm.DepartmentCode.Should().BeEmpty();
        vm.IsLoading.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
        vm.ConnectCommand.Should().NotBeNull();
        vm.UseDemoCommand.Should().NotBeNull();
    }

    [Fact]
    public void UseDemoCommand_SetsDemoCode()
    {
        var vm = _services.GetService<EnrollmentViewModel>();

        vm.UseDemoCommand.Execute(null);

        vm.DepartmentCode.Should().Be("DEMO");
    }

    [Fact]
    public void ConnectCommand_WithEmptyCode_SetsError()
    {
        var vm = _services.GetService<EnrollmentViewModel>();
        vm.DepartmentCode = "";

        vm.ConnectCommand.Execute(null);

        vm.ErrorMessage.Should().Contain("enter a department code");
    }

    [Fact]
    public void ConnectCommand_WithShortCode_SetsError()
    {
        var vm = _services.GetService<EnrollmentViewModel>();
        vm.DepartmentCode = "X";

        vm.ConnectCommand.Execute(null);

        vm.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public void ConnectCommand_WithValidCode_CallsSetDepartmentCode()
    {
        var vm = _services.GetService<EnrollmentViewModel>();
        vm.DepartmentCode = "SPRINGFIELD-PD";
        var eventRaised = false;
        vm.EnrollmentComplete += (s, e) => eventRaised = true;

        vm.ConnectCommand.Execute(null);

        _services.MockAuth.Verify(x => x.SetDepartmentCode("SPRINGFIELD-PD"), Moq.Times.Once);
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void DepartmentCode_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<EnrollmentViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.DepartmentCode))
                propertyChanged = true;
        };

        vm.DepartmentCode = "NEW-CODE";

        propertyChanged.Should().BeTrue();
    }
}
