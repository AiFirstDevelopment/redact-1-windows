using FluentAssertions;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class ViewModelBaseTests
{
    [Fact]
    public void IsLoading_PropertyChanged_RaisesEvent()
    {
        var vm = new TestViewModel();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsLoading))
                propertyChanged = true;
        };

        vm.IsLoading = true;

        propertyChanged.Should().BeTrue();
        vm.IsLoading.Should().BeTrue();
    }

    [Fact]
    public void ErrorMessage_SetError_SetsMessage()
    {
        var vm = new TestViewModel();

        vm.TestSetError("Test error");

        vm.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public void ErrorMessage_SetErrorWithException_SetsMessage()
    {
        var vm = new TestViewModel();

        vm.TestSetError(new Exception("Exception message"));

        vm.ErrorMessage.Should().Be("Exception message");
    }

    [Fact]
    public void ErrorMessage_ClearError_ClearsMessage()
    {
        var vm = new TestViewModel();
        vm.TestSetError("Test error");

        vm.TestClearError();

        vm.ErrorMessage.Should().BeNull();
    }

    private class TestViewModel : ViewModelBase
    {
        public void TestSetError(string message) => SetError(message);
        public void TestSetError(Exception ex) => SetError(ex);
        public void TestClearError() => ClearError();
    }
}

public class RelayCommandTests
{
    [Fact]
    public void Execute_CallsAction()
    {
        var called = false;
        var command = new RelayCommand(() => called = true);

        command.Execute(null);

        called.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithoutPredicate_ReturnsTrue()
    {
        var command = new RelayCommand(() => { });

        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithPredicate_ReturnsPredicate()
    {
        var canExecute = false;
        var command = new RelayCommand(() => { }, () => canExecute);

        command.CanExecute(null).Should().BeFalse();

        canExecute = true;
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RaiseCanExecuteChanged_RaisesEvent()
    {
        var command = new RelayCommand(() => { });
        var eventRaised = false;
        command.CanExecuteChanged += (s, e) => eventRaised = true;

        command.RaiseCanExecuteChanged();

        eventRaised.Should().BeTrue();
    }
}

public class RelayCommandGenericTests
{
    [Fact]
    public void Execute_CallsActionWithParameter()
    {
        string? received = null;
        var command = new RelayCommand<string>(s => received = s);

        command.Execute("test");

        received.Should().Be("test");
    }

    [Fact]
    public void CanExecute_WithoutPredicate_ReturnsTrue()
    {
        var command = new RelayCommand<string>(s => { });

        command.CanExecute("test").Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithPredicate_ReturnsPredicate()
    {
        var command = new RelayCommand<string>(s => { }, s => s == "valid");

        command.CanExecute("valid").Should().BeTrue();
        command.CanExecute("invalid").Should().BeFalse();
    }
}

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_CallsAsyncAction()
    {
        var called = false;
        var command = new AsyncRelayCommand(async () =>
        {
            await Task.Delay(10);
            called = true;
        });

        command.Execute(null);
        await Task.Delay(50);

        called.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WhileExecuting_ReturnsFalse()
    {
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);

        command.CanExecute(null).Should().BeTrue();
        command.Execute(null);
        command.CanExecute(null).Should().BeFalse();

        tcs.SetResult();
    }

    [Fact]
    public async Task Execute_RaisesCanExecuteChangedTwice()
    {
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);
        var raiseCount = 0;
        command.CanExecuteChanged += (s, e) => raiseCount++;

        command.Execute(null);
        raiseCount.Should().Be(1);

        tcs.SetResult();
        await Task.Delay(50);
        raiseCount.Should().Be(2);
    }

    [Fact]
    public void Execute_WhenAlreadyExecuting_DoesNotExecuteAgain()
    {
        var executeCount = 0;
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() =>
        {
            executeCount++;
            return tcs.Task;
        });

        command.Execute(null);
        command.Execute(null);
        command.Execute(null);

        executeCount.Should().Be(1);
        tcs.SetResult();
    }
}

public class AsyncRelayCommandGenericTests
{
    [Fact]
    public async Task Execute_CallsAsyncActionWithParameter()
    {
        string? received = null;
        var command = new AsyncRelayCommand<string>(async s =>
        {
            await Task.Delay(10);
            received = s;
        });

        command.Execute("test");
        await Task.Delay(50);

        received.Should().Be("test");
    }

    [Fact]
    public void CanExecute_WhileExecuting_ReturnsFalse()
    {
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand<string>(s => tcs.Task);

        command.CanExecute("test").Should().BeTrue();
        command.Execute("test");
        command.CanExecute("test").Should().BeFalse();

        tcs.SetResult();
    }
}
