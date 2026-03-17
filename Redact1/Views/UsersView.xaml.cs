using Microsoft.Extensions.DependencyInjection;
using Redact1.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Redact1.Views
{
    public partial class UsersView : UserControl
    {
        private UsersViewModel? _viewModel;

        public UsersView()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            _viewModel = App.Services.GetRequiredService<UsersViewModel>();
            DataContext = _viewModel;
            _ = _viewModel.LoadUsersAsync();
        }

        private void EditPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EditPassword = EditPasswordBox.Password;
            }
        }
    }
}
