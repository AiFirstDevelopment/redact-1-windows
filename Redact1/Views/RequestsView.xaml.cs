using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Redact1.Views
{
    public partial class RequestsView : UserControl
    {
        private RequestsViewModel? _viewModel;

        public event EventHandler<RecordsRequest>? RequestSelected;

        public RequestsView()
        {
            InitializeComponent();
        }

        public void Initialize(bool showArchived = false)
        {
            _viewModel = App.Services.GetRequiredService<RequestsViewModel>();
            _viewModel.ShowArchived = showArchived;
            _viewModel.RequestSelected += (s, r) => RequestSelected?.Invoke(this, r);

            DataContext = _viewModel;

            TitleText.Text = showArchived ? "Archived Requests" : "Records Requests";
            CreateButton.Visibility = showArchived ? Visibility.Collapsed : Visibility.Visible;
            StatusFilter.Visibility = showArchived ? Visibility.Collapsed : Visibility.Visible;

            _ = _viewModel.LoadRequestsAsync();
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.CreateRequestCommand.ExecuteAsync(null);
            }
        }

        private void RequestItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RecordsRequest request)
            {
                _viewModel?.OpenRequestCommand.Execute(request);
            }
        }
    }
}
