using Redact1.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Redact1.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                await vm.LoginCommand.ExecuteAsync(null);
            }
        }
    }
}
