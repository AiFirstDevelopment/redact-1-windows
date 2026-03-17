using CommunityToolkit.Mvvm.ComponentModel;

namespace Redact1.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        protected void ClearError() => ErrorMessage = null;

        protected void SetError(string message) => ErrorMessage = message;

        protected void SetError(Exception ex) => ErrorMessage = ex.Message;
    }
}
