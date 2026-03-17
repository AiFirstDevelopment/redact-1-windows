using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private object? _currentContent;

        public bool IsSupervisor => CurrentUser?.IsSupervisor ?? false;

        public event EventHandler? LoggedOut;

        public MainViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
            CurrentUser = _authService.CurrentUser;

            _authService.AuthStateChanged += (s, user) =>
            {
                CurrentUser = user;
                OnPropertyChanged(nameof(IsSupervisor));
                if (user == null)
                {
                    LoggedOut?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            IsLoading = true;
            try
            {
                await _authService.LogoutAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateToRequests()
        {
            SelectedTabIndex = 0;
        }

        [RelayCommand]
        private void NavigateToArchived()
        {
            SelectedTabIndex = 1;
        }

        [RelayCommand]
        private void NavigateToUsers()
        {
            if (IsSupervisor)
            {
                SelectedTabIndex = 2;
            }
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            SelectedTabIndex = IsSupervisor ? 3 : 2;
        }
    }
}
