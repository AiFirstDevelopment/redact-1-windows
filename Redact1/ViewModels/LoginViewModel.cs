using Microsoft.Extensions.DependencyInjection;
using Redact1.Services;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private string _email = string.Empty;
        private string _password = string.Empty;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand UseDemoCredentialsCommand { get; }

        public event EventHandler? LoginSucceeded;

        public LoginViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
            LoginCommand = new AsyncRelayCommand(LoginAsync);
            UseDemoCredentialsCommand = new RelayCommand(UseDemoCredentials);
        }

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
                await _authService.LoginAsync(Email, Password, false);
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

        private void UseDemoCredentials()
        {
            Email = "supervisor@test.com";
            Password = "test123";
        }
    }
}
