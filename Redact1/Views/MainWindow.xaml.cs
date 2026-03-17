using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using Redact1.ViewModels;
using System.Windows;

namespace Redact1.Views
{
    public partial class MainWindow : Window
    {
        private readonly IAuthService _authService;
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _authService = App.Services.GetRequiredService<IAuthService>();
            _authService.AuthStateChanged += OnAuthStateChanged;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Try to restore session
            var restored = await _authService.TryRestoreSessionAsync();

            if (restored)
            {
                ShowMainContent();
            }
            else
            {
                ShowLogin();
            }
        }

        private void OnAuthStateChanged(object? sender, User? user)
        {
            Dispatcher.Invoke(() =>
            {
                if (user != null)
                {
                    ShowMainContent();
                }
                else
                {
                    ShowLogin();
                }
            });
        }

        private void ShowLogin()
        {
            LoginView.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
            DetailPanel.Visibility = Visibility.Collapsed;
            FileReviewPanel.Visibility = Visibility.Collapsed;

            var loginViewModel = App.Services.GetRequiredService<LoginViewModel>();
            loginViewModel.LoginSucceeded += (s, e) => ShowMainContent();
            LoginView.DataContext = loginViewModel;
        }

        private void ShowMainContent()
        {
            LoginView.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;

            _viewModel = App.Services.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;

            UserNameText.Text = _authService.CurrentUser?.Name ?? "User";

            // Hide Users tab for non-supervisors
            UsersTab.Visibility = _authService.CurrentUser?.IsSupervisor == true
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Initialize views
            InitializeRequestsView(RequestsView, false);
            InitializeRequestsView(ArchivedView, true);
            UsersView.Initialize();
            SettingsView.Initialize();
        }

        private void InitializeRequestsView(RequestsView view, bool showArchived)
        {
            view.Initialize(showArchived);
            view.RequestSelected += OnRequestSelected;
        }

        private void OnRequestSelected(object? sender, RecordsRequest request)
        {
            DetailPanel.Visibility = Visibility.Visible;
            RequestDetailView.LoadRequest(request.Id);
            RequestDetailView.FileSelected += OnFileSelected;
            RequestDetailView.RequestClosed += OnRequestClosed;
        }

        private void OnRequestClosed(object? sender, EventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            RequestDetailView.FileSelected -= OnFileSelected;
            RequestDetailView.RequestClosed -= OnRequestClosed;
        }

        private void OnFileSelected(object? sender, EvidenceFile file)
        {
            FileReviewPanel.Visibility = Visibility.Visible;
            FileReviewView.LoadFile(file.Id);
            FileReviewView.FileClosed += OnFileClosed;
        }

        private void OnFileClosed(object? sender, EventArgs e)
        {
            FileReviewPanel.Visibility = Visibility.Collapsed;
            FileReviewView.FileClosed -= OnFileClosed;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await _authService.LogoutAsync();
        }
    }
}
