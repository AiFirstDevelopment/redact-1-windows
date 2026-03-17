using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.ViewModels;

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

            CloseButton.Click += (s, e) => _viewModel?.CloseCommand.Execute(null);
        }

        public async void LoadFile(string fileId)
        {
            _viewModel = App.Services.GetRequiredService<FileReviewViewModel>();
            _viewModel.FileClosed += (s, e) => FileClosed?.Invoke(this, e);

            DataContext = _viewModel;

            // Subscribe to collection changes to redraw overlays
            _viewModel.Detections.CollectionChanged += (s, e) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(DrawOverlays);
            _viewModel.ManualRedactions.CollectionChanged += (s, e) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(DrawOverlays);

            // Redraw when page changes
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "CurrentPage")
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(DrawOverlays);
                }
            };

            await _viewModel.LoadFileAsync(fileId);

            // Redraw when image bounds change
            DisplayImage.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "Bounds" && DisplayImage.Bounds.Width > 0)
                {
                    UpdateCanvasSize();
                    DrawOverlays();
                }
            };
        }

        private void UpdateCanvasSize()
        {
            if (DisplayImage.Bounds.Width > 0 && DisplayImage.Bounds.Height > 0)
            {
                OverlayCanvas.Width = DisplayImage.Bounds.Width;
                OverlayCanvas.Height = DisplayImage.Bounds.Height;
            }
        }

        private void DrawOverlays()
        {
            if (_viewModel == null) return;

            OverlayCanvas.Children.Clear();

            var imageWidth = DisplayImage.Bounds.Width;
            var imageHeight = DisplayImage.Bounds.Height;

            Console.WriteLine($"[DrawOverlays] Image size: {imageWidth}x{imageHeight}, Detections: {_viewModel.Detections.Count}, ManualRedactions: {_viewModel.ManualRedactions.Count}");

            if (imageWidth <= 0 || imageHeight <= 0) return;

            var currentPage = _viewModel.CurrentPage;

            // Draw detections for current page only
            var pageDetections = _viewModel.Detections.Where(d =>
                d.HasBoundingBox &&
                (d.PageNumber == null || d.PageNumber == currentPage)).ToList();

            Console.WriteLine($"[DrawOverlays] Page {currentPage} has {pageDetections.Count} detections to draw");

            foreach (var detection in pageDetections)
            {
                var rectWidth = detection.BboxWidth!.Value * imageWidth;
                var rectHeight = detection.BboxHeight!.Value * imageHeight;
                // Y flip: PDF uses bottom-left origin, screen uses top-left
                var rectX = detection.BboxX!.Value * imageWidth;
                var rectY = (1 - detection.BboxY!.Value - detection.BboxHeight!.Value) * imageHeight;

                var rect = new Rectangle
                {
                    Width = rectWidth,
                    Height = rectHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Tag = detection
                };

                rect.PointerPressed += OnRedactionRectClicked;

                Canvas.SetLeft(rect, rectX);
                Canvas.SetTop(rect, rectY);

                OverlayCanvas.Children.Add(rect);
            }

            // Draw manual redactions for current page only
            var pageRedactions = _viewModel.ManualRedactions.Where(r =>
                r.BboxX.HasValue &&
                (r.PageNumber == null || r.PageNumber == currentPage));

            foreach (var redaction in pageRedactions)
            {
                var rectWidth = redaction.BboxWidth!.Value * imageWidth;
                var rectHeight = redaction.BboxHeight!.Value * imageHeight;
                // Y flip: PDF uses bottom-left origin, screen uses top-left
                var rectX = redaction.BboxX!.Value * imageWidth;
                var rectY = (1 - redaction.BboxY!.Value - redaction.BboxHeight!.Value) * imageHeight;

                var rect = new Rectangle
                {
                    Width = rectWidth,
                    Height = rectHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Tag = redaction
                };

                rect.PointerPressed += OnRedactionRectClicked;

                Canvas.SetLeft(rect, rectX);
                Canvas.SetTop(rect, rectY);

                OverlayCanvas.Children.Add(rect);
            }
        }

        private async void OnRedactionRectClicked(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel == null || _viewModel.IsDrawingMode) return;

            if (sender is Rectangle rect && rect.Tag != null)
            {
                e.Handled = true;

                if (rect.Tag is Detection detection)
                {
                    await _viewModel.RejectDetectionAsync(detection);
                }
                else if (rect.Tag is ManualRedaction redaction)
                {
                    await _viewModel.DeleteManualRedactionAsync(redaction);
                }

                DrawOverlays();
            }
        }

        private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel?.IsDrawingMode != true) return;

            _isDrawing = true;
            _startPoint = e.GetPosition(OverlayCanvas);

            _currentRect = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0))
            };

            Canvas.SetLeft(_currentRect, _startPoint.X);
            Canvas.SetTop(_currentRect, _startPoint.Y);

            OverlayCanvas.Children.Add(_currentRect);
            e.Pointer.Capture(OverlayCanvas);
        }

        private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
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

        private async void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDrawing || _currentRect == null || _viewModel == null) return;

            _isDrawing = false;
            e.Pointer.Capture(null);

            var imageWidth = DisplayImage.Bounds.Width;
            var imageHeight = DisplayImage.Bounds.Height;

            if (imageWidth <= 0 || imageHeight <= 0) return;

            var x = Canvas.GetLeft(_currentRect) / imageWidth;
            var y = Canvas.GetTop(_currentRect) / imageHeight;
            var width = _currentRect.Width / imageWidth;
            var height = _currentRect.Height / imageHeight;

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
    }
}
