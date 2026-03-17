using Microsoft.Extensions.DependencyInjection;
using Redact1.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Redact1.Views
{
    public partial class FileReviewView : UserControl
    {
        private FileReviewViewModel? _viewModel;
        private bool _isDrawing;
        private Point _startPoint;
        private Rectangle? _currentRect;

        public event EventHandler? FileClosed;

        public FileReviewView()
        {
            InitializeComponent();
        }

        public async void LoadFile(string fileId)
        {
            _viewModel = App.Services.GetRequiredService<FileReviewViewModel>();
            _viewModel.FileClosed += (s, e) => FileClosed?.Invoke(this, e);

            DataContext = _viewModel;

            await _viewModel.LoadFileAsync(fileId);

            // Draw detection overlays after image loads
            DisplayImage.Loaded += (s, e) => DrawOverlays();
        }

        private void DrawOverlays()
        {
            if (_viewModel == null) return;

            OverlayCanvas.Children.Clear();

            var imageWidth = DisplayImage.ActualWidth;
            var imageHeight = DisplayImage.ActualHeight;

            if (imageWidth <= 0 || imageHeight <= 0) return;

            // Draw detections
            foreach (var detection in _viewModel.Detections)
            {
                if (!detection.HasBoundingBox) continue;

                var rect = new Rectangle
                {
                    Width = detection.BboxWidth!.Value * imageWidth,
                    Height = detection.BboxHeight!.Value * imageHeight,
                    Stroke = GetStatusBrush(detection.Status),
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 255))
                };

                Canvas.SetLeft(rect, detection.BboxX!.Value * imageWidth);
                Canvas.SetTop(rect, detection.BboxY!.Value * imageHeight);

                OverlayCanvas.Children.Add(rect);
            }

            // Draw manual redactions
            foreach (var redaction in _viewModel.ManualRedactions)
            {
                if (!redaction.BboxX.HasValue) continue;

                var rect = new Rectangle
                {
                    Width = redaction.BboxWidth!.Value * imageWidth,
                    Height = redaction.BboxHeight!.Value * imageHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0))
                };

                Canvas.SetLeft(rect, redaction.BboxX!.Value * imageWidth);
                Canvas.SetTop(rect, redaction.BboxY!.Value * imageHeight);

                OverlayCanvas.Children.Add(rect);
            }
        }

        private Brush GetStatusBrush(string status)
        {
            return status switch
            {
                "approved" => Brushes.Green,
                "rejected" => Brushes.Red,
                _ => Brushes.Orange
            };
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.IsDrawingMode != true) return;

            _isDrawing = true;
            _startPoint = e.GetPosition(OverlayCanvas);

            _currentRect = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0))
            };

            Canvas.SetLeft(_currentRect, _startPoint.X);
            Canvas.SetTop(_currentRect, _startPoint.Y);

            OverlayCanvas.Children.Add(_currentRect);
            OverlayCanvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing || _currentRect == null) return;

            var currentPoint = e.GetPosition(OverlayCanvas);

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _startPoint.X);
            var height = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(_currentRect, x);
            Canvas.SetTop(_currentRect, y);
            _currentRect.Width = width;
            _currentRect.Height = height;
        }

        private async void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing || _currentRect == null || _viewModel == null) return;

            _isDrawing = false;
            OverlayCanvas.ReleaseMouseCapture();

            var imageWidth = DisplayImage.ActualWidth;
            var imageHeight = DisplayImage.ActualHeight;

            if (imageWidth <= 0 || imageHeight <= 0) return;

            // Convert to normalized coordinates
            var x = Canvas.GetLeft(_currentRect) / imageWidth;
            var y = Canvas.GetTop(_currentRect) / imageHeight;
            var width = _currentRect.Width / imageWidth;
            var height = _currentRect.Height / imageHeight;

            // Only add if meaningful size
            if (width > 0.01 && height > 0.01)
            {
                await _viewModel.AddManualRedaction(x, y, width, height);
                DrawOverlays();
            }
            else
            {
                OverlayCanvas.Children.Remove(_currentRect);
            }

            _currentRect = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.CloseCommand.Execute(null);
        }
    }
}
