using Microsoft.Extensions.DependencyInjection;
using Redact1.Services;
using Redact1.ViewModels;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Redact1
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static AppSettings Settings { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoadSettings();
            ConfigureServices();
        }

        private void LoadSettings()
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                Settings = new AppSettings();
            }
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri(Settings.ApiSettings.BaseUrl);
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IDetectionService, DetectionService>();
            services.AddSingleton<IRedactionService, RedactionService>();
            services.AddSingleton<IStorageService, StorageService>();

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<RequestsViewModel>();
            services.AddTransient<RequestDetailViewModel>();
            services.AddTransient<FileReviewViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<SettingsViewModel>();

            Services = services.BuildServiceProvider();
        }
    }

    public class AppSettings
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public StorageKeys StorageKeys { get; set; } = new();
    }

    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "https://redact-1-worker.joelstevick.workers.dev";
    }

    public class StorageKeys
    {
        public string AuthToken { get; set; } = "redact1_auth_token";
        public string User { get; set; } = "redact1_user";
        public string AgencyConfig { get; set; } = "redact1_agency_config";
    }
}
