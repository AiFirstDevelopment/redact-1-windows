using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class FileReviewViewModel : ViewModelBase, IDisposable
    {
        private readonly IApiService _apiService;
        private readonly IDetectionService _detectionService;
        private readonly IRedactionService _redactionService;

        private byte[]? _originalFileData;
        private byte[]? _redactedFileData;
        private MemoryStream? _displayImageStream;
        private MemoryStream? _redactedImageStream;

        private EvidenceFile? _file;
        private ObservableCollection<Detection> _detections = new();
        private ObservableCollection<ManualRedaction> _manualRedactions = new();
        private Detection? _selectedDetection;
        private Bitmap? _displayImage;
        private Bitmap? _redactedImage;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private bool _isDetecting;
        private bool _showRedacted;
        private bool _isDrawingMode;

        public EvidenceFile? File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        public ObservableCollection<Detection> Detections
        {
            get => _detections;
            set => SetProperty(ref _detections, value);
        }

        public ObservableCollection<ManualRedaction> ManualRedactions
        {
            get => _manualRedactions;
            set => SetProperty(ref _manualRedactions, value);
        }

        public Detection? SelectedDetection
        {
            get => _selectedDetection;
            set => SetProperty(ref _selectedDetection, value);
        }

        public Bitmap? DisplayImage
        {
            get => _displayImage;
            set => SetProperty(ref _displayImage, value);
        }

        public Bitmap? RedactedImage
        {
            get => _redactedImage;
            set => SetProperty(ref _redactedImage, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
        }

        public bool ShowRedacted
        {
            get => _showRedacted;
            set => SetProperty(ref _showRedacted, value);
        }

        public bool IsDrawingMode
        {
            get => _isDrawingMode;
            set => SetProperty(ref _isDrawingMode, value);
        }

        public ICommand LoadDetectionsCommand { get; }
        public ICommand RunDetectionCommand { get; }
        public ICommand ApproveDetectionCommand { get; }
        public ICommand RejectDetectionCommand { get; }
        public ICommand ApproveAllCommand { get; }
        public ICommand DeleteManualRedactionCommand { get; }
        public ICommand PreviewRedactedCommand { get; }
        public ICommand SaveRedactedCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ToggleDrawingModeCommand { get; }
        public ICommand CloseCommand { get; }

        public event EventHandler? FileClosed;

        public FileReviewViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
            _detectionService = App.Services.GetRequiredService<IDetectionService>();
            _redactionService = App.Services.GetRequiredService<IRedactionService>();

            LoadDetectionsCommand = new AsyncRelayCommand(LoadDetectionsAsync);
            RunDetectionCommand = new AsyncRelayCommand(RunDetectionAsync);
            ApproveDetectionCommand = new AsyncRelayCommand<Detection>(ApproveDetectionAsync);
            RejectDetectionCommand = new AsyncRelayCommand<Detection>(RejectDetectionAsync);
            ApproveAllCommand = new AsyncRelayCommand(ApproveAllAsync);
            DeleteManualRedactionCommand = new AsyncRelayCommand<ManualRedaction>(DeleteManualRedactionAsync);
            PreviewRedactedCommand = new AsyncRelayCommand(PreviewRedactedAsync);
            SaveRedactedCommand = new AsyncRelayCommand(SaveRedactedAsync);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
            ToggleDrawingModeCommand = new RelayCommand(ToggleDrawingMode);
            CloseCommand = new RelayCommand(Close);
        }

        public async Task LoadFileAsync(string fileId)
        {
            IsLoading = true;
            ClearError();

            try
            {
                Console.WriteLine($"[FileReview] Loading file: {fileId}");
                File = await _apiService.GetFileAsync(fileId);
                Console.WriteLine($"[FileReview] Got file metadata: {File?.Filename}");

                _originalFileData = await _apiService.GetOriginalFileAsync(fileId);
                Console.WriteLine($"[FileReview] Got file data: {_originalFileData?.Length ?? 0} bytes");

                if (File.IsPdf)
                {
                    TotalPages = _redactionService.GetPdfPageCount(_originalFileData);
                    Console.WriteLine($"[FileReview] PDF has {TotalPages} pages");
                    await LoadPdfPage(1);
                }
                else
                {
                    await LoadImage();
                }

                await LoadDetectionsAsync();
                Console.WriteLine($"[FileReview] Load complete, {Detections.Count} detections");

                // Auto-detect if no detections exist (like iOS app)
                if (Detections.Count == 0)
                {
                    Console.WriteLine("[FileReview] No detections found, running auto-detection...");
                    await RunDetectionAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileReview] ERROR: {ex.Message}");
                Console.WriteLine($"[FileReview] Stack: {ex.StackTrace}");
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadImage()
        {
            if (_originalFileData == null) return;

            await Task.Run(() =>
            {
                _displayImageStream = new MemoryStream(_originalFileData);
                var bitmap = new Bitmap(_displayImageStream);

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    DisplayImage = bitmap;
                });
            });
        }

        private async Task LoadPdfPage(int pageNumber)
        {
            if (_originalFileData == null) return;

            Console.WriteLine($"[FileReview] Rendering PDF page {pageNumber}");
            var pageImage = await _redactionService.RenderPdfPageToImageAsync(_originalFileData, pageNumber);
            Console.WriteLine($"[FileReview] Got page image: {pageImage?.Length ?? 0} bytes");

            _displayImageStream?.Dispose();
            _displayImageStream = new MemoryStream(pageImage);
            DisplayImage = new Bitmap(_displayImageStream);
            Console.WriteLine($"[FileReview] DisplayImage set, size: {DisplayImage?.PixelSize.Width}x{DisplayImage?.PixelSize.Height}");
            CurrentPage = pageNumber;
        }

        private async Task LoadDetectionsAsync()
        {
            if (File == null) return;

            try
            {
                Console.WriteLine($"[FileReview] Loading detections for file {File.Id}...");
                var result = await _apiService.GetDetectionsAsync(File.Id);
                Console.WriteLine($"[FileReview] API returned {result.Detections.Count} detections, {result.ManualRedactions.Count} manual redactions");

                Detections.Clear();
                foreach (var detection in result.Detections)
                {
                    Console.WriteLine($"[FileReview] Detection: {detection.DetectionType} @ ({detection.BboxX},{detection.BboxY}) - HasBbox: {detection.HasBoundingBox}");
                    Detections.Add(detection);
                }

                ManualRedactions.Clear();
                foreach (var redaction in result.ManualRedactions)
                {
                    ManualRedactions.Add(redaction);
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task RunDetectionAsync()
        {
            if (File == null || _originalFileData == null) return;

            IsDetecting = true;
            ClearError();

            try
            {
                await _apiService.ClearDetectionsAsync(File.Id);
                Detections.Clear();

                List<CreateDetectionRequest> allDetections = new();

                if (File.IsImage)
                {
                    Console.WriteLine("[Detection] Running image detection...");
                    var detected = await _detectionService.DetectInImageAsync(_originalFileData);
                    Console.WriteLine($"[Detection] Image found {detected.Count} detections");
                    allDetections.AddRange(detected);
                }
                else if (File.IsPdf)
                {
                    Console.WriteLine($"[Detection] Running PDF detection on {TotalPages} pages...");
                    for (int page = 1; page <= TotalPages; page++)
                    {
                        Console.WriteLine($"[Detection] Processing page {page}...");
                        var pageImage = await _redactionService.RenderPdfPageToImageAsync(_originalFileData, page);
                        Console.WriteLine($"[Detection] Page {page} rendered, {pageImage?.Length ?? 0} bytes");
                        var detected = await _detectionService.DetectInPdfPageAsync(pageImage, page);
                        Console.WriteLine($"[Detection] Page {page} found {detected.Count} detections");
                        allDetections.AddRange(detected);
                    }
                }

                Console.WriteLine($"[Detection] Total detections: {allDetections.Count}");
                if (allDetections.Count > 0)
                {
                    // Log first detection for debugging
                    var first = allDetections[0];
                    Console.WriteLine($"[Detection] First detection: type={first.DetectionType}, text={first.TextContent}, bbox=({first.BboxX:F3},{first.BboxY:F3},{first.BboxWidth:F3},{first.BboxHeight:F3}), page={first.PageNumber}");
                    Console.WriteLine($"[Detection] Creating detections via API...");
                    var created = await _apiService.CreateDetectionsAsync(File.Id, allDetections);
                    Console.WriteLine($"[Detection] API created {created.Count} detections");
                    foreach (var detection in created)
                    {
                        Console.WriteLine($"[Detection] Created: {detection.Id}, bbox=({detection.BboxX:F3},{detection.BboxY:F3}), HasBbox: {detection.HasBoundingBox}");
                        // Auto-approve detections in background (don't wait for response)
                        _ = _apiService.UpdateDetectionAsync(
                            detection.Id,
                            new UpdateDetectionRequest { Status = "approved" }
                        );
                        // Use the created detection directly (it has bbox)
                        detection.Status = "approved";
                        Detections.Add(detection);
                    }
                    Console.WriteLine($"[Detection] Detections collection now has {Detections.Count} items");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Detection] ERROR: {ex.Message}");
                Console.WriteLine($"[Detection] Stack: {ex.StackTrace}");
                SetError(ex);
            }
            finally
            {
                IsDetecting = false;
            }
        }

        private async Task ApproveDetectionAsync(Detection? detection)
        {
            if (detection == null) return;

            try
            {
                var updated = await _apiService.UpdateDetectionAsync(
                    detection.Id,
                    new UpdateDetectionRequest { Status = "approved" }
                );

                var index = Detections.IndexOf(detection);
                if (index >= 0)
                {
                    Detections[index] = updated;
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        public async Task RejectDetectionAsync(Detection? detection)
        {
            if (detection == null) return;

            try
            {
                var updated = await _apiService.UpdateDetectionAsync(
                    detection.Id,
                    new UpdateDetectionRequest { Status = "rejected" }
                );

                var index = Detections.IndexOf(detection);
                if (index >= 0)
                {
                    Detections[index] = updated;
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task ApproveAllAsync()
        {
            foreach (var detection in Detections.Where(d => d.Status == "pending").ToList())
            {
                await ApproveDetectionAsync(detection);
            }
        }

        public async Task AddManualRedaction(double x, double y, double width, double height)
        {
            if (File == null) return;

            try
            {
                var request = new CreateManualRedactionRequest
                {
                    BboxX = x,
                    BboxY = y,
                    BboxWidth = width,
                    BboxHeight = height,
                    PageNumber = File.IsPdf ? CurrentPage : null
                };

                var redaction = await _apiService.CreateManualRedactionAsync(File.Id, request);
                ManualRedactions.Add(redaction);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        public async Task DeleteManualRedactionAsync(ManualRedaction? redaction)
        {
            if (redaction == null) return;

            try
            {
                await _apiService.DeleteManualRedactionAsync(redaction.Id);
                ManualRedactions.Remove(redaction);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task PreviewRedactedAsync()
        {
            if (File == null || _originalFileData == null) return;

            IsLoading = true;

            try
            {
                byte[] redactedData;

                if (File.IsImage)
                {
                    redactedData = await _redactionService.RedactImageAsync(
                        _originalFileData,
                        Detections.ToList(),
                        ManualRedactions.ToList()
                    );
                }
                else
                {
                    redactedData = await _redactionService.RedactPdfAsync(
                        _originalFileData,
                        Detections.ToList(),
                        ManualRedactions.ToList()
                    );
                }

                _redactedFileData = redactedData;

                if (File.IsImage)
                {
                    _redactedImageStream?.Dispose();
                    _redactedImageStream = new MemoryStream(redactedData);
                    RedactedImage = new Bitmap(_redactedImageStream);
                }

                ShowRedacted = true;
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

        private async Task SaveRedactedAsync()
        {
            if (File == null || _redactedFileData == null) return;

            IsLoading = true;

            try
            {
                var filename = File.IsPdf
                    ? Path.ChangeExtension(File.Filename, ".redacted.pdf")
                    : Path.ChangeExtension(File.Filename, ".redacted.jpg");

                await _apiService.UploadRedactedFileAsync(File.Id, _redactedFileData, filename);
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

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                await LoadPdfPage(CurrentPage + 1);
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                await LoadPdfPage(CurrentPage - 1);
            }
        }

        private void ToggleDrawingMode()
        {
            IsDrawingMode = !IsDrawingMode;
        }

        private void Close()
        {
            FileClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _displayImageStream?.Dispose();
            _redactedImageStream?.Dispose();
        }
    }
}
