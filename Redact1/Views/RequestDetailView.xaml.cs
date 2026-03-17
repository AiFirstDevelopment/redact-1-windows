using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Redact1.Views
{
    public partial class RequestDetailView : UserControl
    {
        private RequestDetailViewModel? _viewModel;

        public event EventHandler<EvidenceFile>? FileSelected;
        public event EventHandler? RequestClosed;

        public RequestDetailView()
        {
            InitializeComponent();
        }

        public async void LoadRequest(string requestId)
        {
            _viewModel = App.Services.GetRequiredService<RequestDetailViewModel>();
            _viewModel.FileSelected += (s, f) => FileSelected?.Invoke(this, f);
            _viewModel.RequestClosed += (s, e) => RequestClosed?.Invoke(this, e);

            DataContext = _viewModel;

            await _viewModel.LoadRequestAsync(requestId);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.CloseCommand.Execute(null);
        }

        private void FileItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is EvidenceFile file)
            {
                _viewModel?.OpenFileCommand.Execute(file);
            }
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // Prevent triggering the parent click
        }
    }
}
