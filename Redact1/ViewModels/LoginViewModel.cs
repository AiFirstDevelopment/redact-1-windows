using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Services;

namespace Redact1.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _useEmployeeId;

        public event EventHandler? LoginSucceeded;

        public LoginViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                SetError("Please enter email and password");
                return;
            }

            ClearError();
            IsLoading = true;

            try
            {
                await _authService.LoginAsync(Email, Password, UseEmployeeId);
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetError($"Login failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void UseDemoCredentials()
        {
            Email = "clerk@pd.local";
            Password = "test-password";
        }
    }
}
