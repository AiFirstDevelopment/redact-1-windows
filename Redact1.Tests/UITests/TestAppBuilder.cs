using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Redact1;
using Redact1.Models;
using Redact1.Services;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;
using Redact1.Views;

namespace Redact1.Tests.UITests;

public class TestAppBuilder : IDisposable
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public Mock<IApiService> MockApi { get; }
    public Mock<IAuthService> MockAuth { get; }
    public Mock<IStorageService> MockStorage { get; }
    public Mock<IDetectionService> MockDetection { get; }
    public Mock<IRedactionService> MockRedaction { get; }
    public IServiceProvider Services { get; }

    public TestAppBuilder(bool isAuthenticated = false, bool isEnrolled = true, bool isSupervisor = false)
    {
        MockApi = MockApiService.Create();
        MockAuth = MockAuthService.Create(isAuthenticated, isEnrolled, isSupervisor);
        MockStorage = MockStorageService.Create();
        MockDetection = MockDetectionService.Create();
        MockRedaction = MockRedactionService.Create();

        var services = new ServiceCollection();

        services.AddSingleton(MockApi.Object);
        services.AddSingleton(MockAuth.Object);
        services.AddSingleton(MockStorage.Object);
        services.AddSingleton(MockDetection.Object);
        services.AddSingleton(MockRedaction.Object);

        services.AddTransient<EnrollmentViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<RequestsViewModel>();
        services.AddTransient<RequestDetailViewModel>();
        services.AddTransient<FileReviewViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<SettingsViewModel>();

        Services = services.BuildServiceProvider();

        // Set up App statics
        App.Services = Services;
        App.Settings = new AppSettings
        {
            ApiSettings = new ApiSettings { BaseUrl = "https://test.local" },
            StorageKeys = new StorageKeys()
        };
    }

    public static void EnsureInitialized()
    {
        lock (_lock)
        {
            if (!_initialized)
            {
                AppBuilder.Configure<TestApp>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
                _initialized = true;
            }
        }
    }

    public T CreateView<T>() where T : new()
    {
        EnsureInitialized();
        return new T();
    }

    public void Dispose()
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

public class TestApp : Application
{
    public override void Initialize()
    {
        // Minimal initialization for headless testing
    }
}
