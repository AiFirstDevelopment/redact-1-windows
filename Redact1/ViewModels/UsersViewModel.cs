using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;

namespace Redact1.ViewModels
{
    public partial class UsersViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editName = string.Empty;

        [ObservableProperty]
        private string _editEmail = string.Empty;

        [ObservableProperty]
        private string _editRole = "clerk";

        [ObservableProperty]
        private string _editPassword = string.Empty;

        public UsersViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
        }

        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            ClearError();

            try
            {
                var users = await _apiService.GetUsersAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void StartCreateUser()
        {
            SelectedUser = null;
            EditName = string.Empty;
            EditEmail = string.Empty;
            EditRole = "clerk";
            EditPassword = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        private void StartEditUser(User user)
        {
            SelectedUser = user;
            EditName = user.Name;
            EditEmail = user.Email;
            EditRole = user.Role;
            EditPassword = string.Empty;
            IsEditing = true;
        }

        [RelayCommand]
        private async Task SaveUserAsync()
        {
            if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditEmail))
            {
                SetError("Name and email are required");
                return;
            }

            IsLoading = true;

            try
            {
                if (SelectedUser == null)
                {
                    // Create new user
                    if (string.IsNullOrWhiteSpace(EditPassword))
                    {
                        SetError("Password is required for new users");
                        return;
                    }

                    var request = new CreateUserRequest
                    {
                        Name = EditName,
                        Email = EditEmail,
                        Role = EditRole,
                        Password = EditPassword
                    };

                    var user = await _apiService.CreateUserAsync(request);
                    Users.Add(user);
                }
                else
                {
                    // Update existing user
                    var request = new UpdateUserRequest
                    {
                        Name = EditName,
                        Email = EditEmail,
                        Role = EditRole
                    };

                    if (!string.IsNullOrWhiteSpace(EditPassword))
                    {
                        request.Password = EditPassword;
                    }

                    var user = await _apiService.UpdateUserAsync(SelectedUser.Id, request);
                    var index = Users.IndexOf(SelectedUser);
                    if (index >= 0)
                    {
                        Users[index] = user;
                    }
                }

                IsEditing = false;
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedUser = null;
        }

        [RelayCommand]
        private async Task DeleteUserAsync(User user)
        {
            try
            {
                await _apiService.DeleteUserAsync(user.Id);
                Users.Remove(user);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }
    }
}
