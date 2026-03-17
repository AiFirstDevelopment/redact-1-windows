using Microsoft.Extensions.DependencyInjection;
using Redact1.Services;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class EnrollmentViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private string _departmentCode = string.Empty;

        public string DepartmentCode
        {
            get => _departmentCode;
            set => SetProperty(ref _departmentCode, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand UseDemoCommand { get; }

        public event EventHandler? EnrollmentComplete;

        public EnrollmentViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            UseDemoCommand = new RelayCommand(UseDemo);
        }

        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(DepartmentCode))
            {
                SetError("Please enter a department code");
                return;
            }

            ClearError();
            IsLoading = true;

            try
            {
                // Validate the department code
                var normalizedCode = DepartmentCode.Trim().ToUpperInvariant();

                // For now, accept any code that looks valid (demo mode)
                // In production, this would verify against a backend service
                if (normalizedCode.Length < 2)
                {
                    SetError("Invalid department code");
                    return;
                }

                // Store the department code
                _authService.SetDepartmentCode(normalizedCode);

                EnrollmentComplete?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetError($"Connection failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UseDemo()
        {
            DepartmentCode = "SPRINGFIELD-PD";
        }
    }
}
