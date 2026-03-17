using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Reflection;

namespace Redact1.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        public event EventHandler? LoggedOut;

        public SettingsViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
            CurrentUser = _authService.CurrentUser;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            IsLoading = true;

            try
            {
                await _authService.LogoutAsync();
                LoggedOut?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenSupport()
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "mailto:support@redact1.com",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }

        [RelayCommand]
        private void OpenAbout()
        {
            System.Windows.MessageBox.Show(
                $"Redact-1 for Windows\n{AppVersion}\n\nPolice records redaction system for FOIA requests.",
                "About Redact-1",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information
            );
        }
    }
}
