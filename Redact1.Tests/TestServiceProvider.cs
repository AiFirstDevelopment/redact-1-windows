using Microsoft.Extensions.DependencyInjection;
using Moq;
using Redact1;
using Redact1.Services;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests;

public class TestServiceProvider : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public Mock<IApiService> MockApi { get; }
    public Mock<IAuthService> MockAuth { get; }
    public Mock<IStorageService> MockStorage { get; }
    public Mock<IDetectionService> MockDetection { get; }
    public Mock<IRedactionService> MockRedaction { get; }

    public TestServiceProvider(
        bool isAuthenticated = false,
        bool isEnrolled = true,
        bool isSupervisor = false)
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

        _serviceProvider = services.BuildServiceProvider();
    }

    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public void SetupApp()
    {
        // Replace App.Services and Settings with our test provider
        App.Services = _serviceProvider;
        App.Settings = new AppSettings
        {
            ApiSettings = new ApiSettings { BaseUrl = "https://test.local" },
            StorageKeys = new StorageKeys()
        };
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
