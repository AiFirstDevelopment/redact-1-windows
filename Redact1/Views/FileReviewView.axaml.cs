using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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

        // Selection and manipulation state
        private Rectangle? _selectedRect;
        private object? _selectedTag; // Detection or ManualRedaction
        private bool _isDragging;
        private bool _isResizing;
        private Point _dragStartPoint;
        private Point _originalRectPosition;
        private Size _originalRectSize;
        private string? _resizeHandle; // "nw", "n", "ne", "e", "se", "s", "sw", "w"
        private List<Rectangle> _resizeHandles = new();

        // Long press detection
        private System.Timers.Timer? _longPressTimer;
        private bool _hasMoved;
        private const int LongPressDelayMs = 500;

        public event EventHandler? FileClosed;

        public FileReviewView()
        {
            InitializeComponent();

            CloseButton.Click += (s, e) => _viewModel?.CloseCommand.Execute(null);
            CancelButton.Click += OnCancelClick;
        }

        private async void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // If no changes, just close without confirmation
            if (_viewModel == null || !_viewModel.HasChanges)
            {
                _viewModel?.CancelCommand.Execute(null);
                return;
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                "Discard Changes?",
                "Are you sure you want to cancel? All changes will be lost.",
                ButtonEnum.YesNo,
                Icon.Question);

            var result = await box.ShowAsync();
            if (result == ButtonResult.Yes)
            {
                _viewModel?.CancelCommand.Execute(null);
            }
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

                var isPending = detection.Status == "pending";

                var rect = new Rectangle
                {
                    Width = rectWidth,
                    Height = rectHeight,
                    Stroke = isPending ? Brushes.Orange : Brushes.Black,
                    StrokeThickness = 2,
                    StrokeDashArray = isPending ? new Avalonia.Collections.AvaloniaList<double> { 4, 2 } : null,
                    Fill = new SolidColorBrush(isPending ? Color.FromArgb(40, 255, 165, 0) : Color.FromArgb(80, 0, 0, 0)),
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

        private void OnRedactionRectClicked(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel == null || _viewModel.IsDrawingMode) return;

            if (sender is Rectangle rect && rect.Tag != null)
            {
                e.Handled = true;
                var props = e.GetCurrentPoint(OverlayCanvas).Properties;

                // Right-click: show delete option
                if (props.IsRightButtonPressed)
                {
                    SelectRect(rect);
                    ShowDeleteContextMenu(rect, e.GetPosition(this));
                    return;
                }

                // Double-click: delete
                if (e.ClickCount == 2)
                {
                    _ = DeleteSelectedAsync(rect.Tag);
                    return;
                }

                // Single click: select and prepare for drag
                SelectRect(rect);
                _isDragging = true;
                _hasMoved = false;
                _dragStartPoint = e.GetPosition(OverlayCanvas);
                _originalRectPosition = new Point(Canvas.GetLeft(rect), Canvas.GetTop(rect));
                _originalRectSize = new Size(rect.Width, rect.Height);

                // Approve pending detection on click
                _ = ApproveIfPendingAsync(rect.Tag, rect);

                // Start long press timer
                _longPressTimer?.Stop();
                _longPressTimer = new System.Timers.Timer(LongPressDelayMs);
                _longPressTimer.Elapsed += OnLongPressElapsed;
                _longPressTimer.AutoReset = false;
                _longPressTimer.Start();

                e.Pointer.Capture(OverlayCanvas);
            }
        }

        private void SelectRect(Rectangle rect)
        {
            // Clear previous selection
            ClearSelection();

            _selectedRect = rect;
            _selectedTag = rect.Tag;

            // Highlight selected rect
            rect.StrokeThickness = 3;
            rect.Stroke = Brushes.Blue;

            // Draw resize handles
            DrawResizeHandles(rect);
        }

        private async Task ApproveIfPendingAsync(object? tag, Rectangle? rect)
        {
            if (_viewModel == null || tag is not Detection detection || detection.Status != "pending")
                return;

            // Update in API
            await _viewModel.ApproveDetectionAsync(detection);

            // Update visual appearance immediately
            if (rect != null)
            {
                rect.Stroke = Brushes.Blue; // Keep blue while selected
                rect.StrokeDashArray = null;
                rect.Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            }
        }

        private void ClearSelection()
        {
            if (_selectedRect != null)
            {
                // Restore appearance based on status
                var isPending = _selectedTag is Detection d && d.Status == "pending";
                _selectedRect.StrokeThickness = 2;
                _selectedRect.Stroke = isPending ? Brushes.Orange : Brushes.Black;
            }

            // Remove resize handles
            foreach (var handle in _resizeHandles)
            {
                OverlayCanvas.Children.Remove(handle);
            }
            _resizeHandles.Clear();

            _selectedRect = null;
            _selectedTag = null;
        }

        private void DrawResizeHandles(Rectangle rect)
        {
            const double handleSize = 8;
            var x = Canvas.GetLeft(rect);
            var y = Canvas.GetTop(rect);
            var w = rect.Width;
            var h = rect.Height;

            // Handle positions: corners and edges
            var positions = new (double x, double y, string name)[]
            {
                (x - handleSize/2, y - handleSize/2, "nw"),
                (x + w/2 - handleSize/2, y - handleSize/2, "n"),
                (x + w - handleSize/2, y - handleSize/2, "ne"),
                (x + w - handleSize/2, y + h/2 - handleSize/2, "e"),
                (x + w - handleSize/2, y + h - handleSize/2, "se"),
                (x + w/2 - handleSize/2, y + h - handleSize/2, "s"),
                (x - handleSize/2, y + h - handleSize/2, "sw"),
                (x - handleSize/2, y + h/2 - handleSize/2, "w"),
            };

            foreach (var (hx, hy, name) in positions)
            {
                var handle = new Rectangle
                {
                    Width = handleSize,
                    Height = handleSize,
                    Fill = Brushes.White,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                    Tag = name,
                    Cursor = GetResizeCursor(name)
                };

                handle.PointerPressed += OnResizeHandlePressed;

                Canvas.SetLeft(handle, hx);
                Canvas.SetTop(handle, hy);

                OverlayCanvas.Children.Add(handle);
                _resizeHandles.Add(handle);
            }
        }

        private Cursor GetResizeCursor(string handle)
        {
            return handle switch
            {
                "nw" or "se" => new Cursor(StandardCursorType.TopLeftCorner),
                "ne" or "sw" => new Cursor(StandardCursorType.TopRightCorner),
                "n" or "s" => new Cursor(StandardCursorType.SizeNorthSouth),
                "e" or "w" => new Cursor(StandardCursorType.SizeWestEast),
                _ => new Cursor(StandardCursorType.Arrow)
            };
        }

        private void OnResizeHandlePressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Rectangle handle && _selectedRect != null)
            {
                e.Handled = true;
                _isResizing = true;
                _resizeHandle = handle.Tag as string;
                _dragStartPoint = e.GetPosition(OverlayCanvas);
                _originalRectPosition = new Point(Canvas.GetLeft(_selectedRect), Canvas.GetTop(_selectedRect));
                _originalRectSize = new Size(_selectedRect.Width, _selectedRect.Height);

                // Approve pending detection on resize start
                _ = ApproveIfPendingAsync(_selectedTag, _selectedRect);

                e.Pointer.Capture(OverlayCanvas);
            }
        }

        private void OnLongPressElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_hasMoved && _selectedTag != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_selectedRect != null)
                    {
                        ShowDeleteContextMenu(_selectedRect, new Point(
                            Canvas.GetLeft(_selectedRect) + _selectedRect.Width / 2,
                            Canvas.GetTop(_selectedRect) + _selectedRect.Height / 2));
                    }
                });
            }
            _longPressTimer?.Stop();
        }

        private void ShowDeleteContextMenu(Rectangle rect, Point position)
        {
            var menu = new ContextMenu();
            var deleteItem = new MenuItem { Header = "Delete Redaction" };
            deleteItem.Click += async (s, e) =>
            {
                await DeleteSelectedAsync(rect.Tag);
            };
            menu.Items.Add(deleteItem);

            menu.Open(this);
        }

        private async Task DeleteSelectedAsync(object? tag)
        {
            if (_viewModel == null || tag == null) return;

            ClearSelection();

            if (tag is Detection detection)
            {
                await _viewModel.RejectDetectionAsync(detection);
            }
            else if (tag is ManualRedaction redaction)
            {
                await _viewModel.DeleteManualRedactionAsync(redaction);
            }

            DrawOverlays();
        }

        private void UpdateRectFromDrag(Point currentPoint)
        {
            if (_selectedRect == null) return;

            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;

            if (_isResizing && _resizeHandle != null)
            {
                // Handle resize
                var newX = _originalRectPosition.X;
                var newY = _originalRectPosition.Y;
                var newW = _originalRectSize.Width;
                var newH = _originalRectSize.Height;

                if (_resizeHandle.Contains('w'))
                {
                    newX = _originalRectPosition.X + deltaX;
                    newW = _originalRectSize.Width - deltaX;
                }
                if (_resizeHandle.Contains('e'))
                {
                    newW = _originalRectSize.Width + deltaX;
                }
                if (_resizeHandle.Contains('n'))
                {
                    newY = _originalRectPosition.Y + deltaY;
                    newH = _originalRectSize.Height - deltaY;
                }
                if (_resizeHandle.Contains('s'))
                {
                    newH = _originalRectSize.Height + deltaY;
                }

                // Minimum size
                if (newW >= 10 && newH >= 10)
                {
                    Canvas.SetLeft(_selectedRect, newX);
                    Canvas.SetTop(_selectedRect, newY);
                    _selectedRect.Width = newW;
                    _selectedRect.Height = newH;

                    // Update handle positions
                    foreach (var handle in _resizeHandles)
                    {
                        OverlayCanvas.Children.Remove(handle);
                    }
                    _resizeHandles.Clear();
                    DrawResizeHandles(_selectedRect);
                }
            }
            else if (_isDragging)
            {
                // Handle move
                Canvas.SetLeft(_selectedRect, _originalRectPosition.X + deltaX);
                Canvas.SetTop(_selectedRect, _originalRectPosition.Y + deltaY);

                // Update handle positions
                foreach (var handle in _resizeHandles)
                {
                    OverlayCanvas.Children.Remove(handle);
                }
                _resizeHandles.Clear();
                DrawResizeHandles(_selectedRect);
            }
        }

        private async Task CommitRectChanges()
        {
            if (_selectedRect == null || _selectedTag == null || _viewModel == null) return;

            var imageWidth = DisplayImage.Bounds.Width;
            var imageHeight = DisplayImage.Bounds.Height;

            if (imageWidth <= 0 || imageHeight <= 0) return;

            var newX = Canvas.GetLeft(_selectedRect) / imageWidth;
            var newY = Canvas.GetTop(_selectedRect) / imageHeight;
            var newW = _selectedRect.Width / imageWidth;
            var newH = _selectedRect.Height / imageHeight;

            // Convert Y back to PDF coordinates (flip)
            var pdfY = 1 - newY - newH;

            if (_selectedTag is Detection detection)
            {
                detection.BboxX = newX;
                detection.BboxY = pdfY;
                detection.BboxWidth = newW;
                detection.BboxHeight = newH;
                await _viewModel.UpdateDetectionAsync(detection);
            }
            else if (_selectedTag is ManualRedaction redaction)
            {
                redaction.BboxX = newX;
                redaction.BboxY = pdfY;
                redaction.BboxWidth = newW;
                redaction.BboxHeight = newH;
                await _viewModel.UpdateManualRedactionAsync(redaction);
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
            if (e == null) return;

            var currentPoint = e.GetPosition(OverlayCanvas);

            // Handle drawing new rect
            if (_isDrawing && _currentRect != null)
            {
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(_currentRect, x);
                Canvas.SetTop(_currentRect, y);
                _currentRect.Width = width;
                _currentRect.Height = height;
                return;
            }

            // Handle moving/resizing selected rect
            if ((_isDragging || _isResizing) && _selectedRect != null)
            {
                var delta = currentPoint - _dragStartPoint;
                if (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3)
                {
                    _hasMoved = true;
                    _longPressTimer?.Stop();
                }

                UpdateRectFromDrag(currentPoint);
            }
        }

        private async void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _longPressTimer?.Stop();
            if (e == null) return;

            e.Pointer.Capture(null);

            // Handle drawing new rect
            if (_isDrawing && _currentRect != null && _viewModel != null)
            {
                _isDrawing = false;

                var imageWidth = DisplayImage.Bounds.Width;
                var imageHeight = DisplayImage.Bounds.Height;

                if (imageWidth > 0 && imageHeight > 0)
                {
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
                }

                _currentRect = null;
                return;
            }

            // Handle end of move/resize
            if ((_isDragging || _isResizing) && _hasMoved && _selectedRect != null)
            {
                await CommitRectChanges();

                // Set approved appearance after drag/resize
                _selectedRect.StrokeThickness = 2;
                _selectedRect.Stroke = Brushes.Black;
                _selectedRect.StrokeDashArray = null;
                _selectedRect.Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

                // Remove resize handles and clear selection
                foreach (var handle in _resizeHandles)
                {
                    OverlayCanvas.Children.Remove(handle);
                }
                _resizeHandles.Clear();
                _selectedRect = null;
                _selectedTag = null;
            }

            // If it was just a click (no movement), show approved state immediately
            else if (_isDragging && !_hasMoved && _selectedRect != null)
            {
                // Set approved appearance directly (don't wait for async)
                _selectedRect.StrokeThickness = 2;
                _selectedRect.Stroke = Brushes.Black;
                _selectedRect.StrokeDashArray = null;
                _selectedRect.Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

                // Remove resize handles
                foreach (var handle in _resizeHandles)
                {
                    OverlayCanvas.Children.Remove(handle);
                }
                _resizeHandles.Clear();
                _selectedRect = null;
                _selectedTag = null;
            }

            _isDragging = false;
            _isResizing = false;
            _resizeHandle = null;
        }

        private void Canvas_PointerPressed_Background(object? sender, PointerPressedEventArgs e)
        {
            // Click on background clears selection
            if (!_viewModel?.IsDrawingMode == true && _selectedRect != null)
            {
                var pos = e.GetPosition(OverlayCanvas);
                var hitRect = false;

                foreach (var child in OverlayCanvas.Children)
                {
                    if (child is Rectangle rect && rect != _selectedRect && !_resizeHandles.Contains(rect))
                    {
                        var left = Canvas.GetLeft(rect);
                        var top = Canvas.GetTop(rect);
                        if (pos.X >= left && pos.X <= left + rect.Width &&
                            pos.Y >= top && pos.Y <= top + rect.Height)
                        {
                            hitRect = true;
                            break;
                        }
                    }
                }

                if (!hitRect)
                {
                    ClearSelection();
                }
            }
        }
    }
}
