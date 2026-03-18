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
        private bool _showDetectionPrompt = true;
        private bool _hasUnsavedChanges;

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

        public bool ShowDetectionPrompt
        {
            get => _showDetectionPrompt;
            set => SetProperty(ref _showDetectionPrompt, value);
        }

        public bool HasChanges => _hasUnsavedChanges;

        public ICommand LoadDetectionsCommand { get; }
        public ICommand RunDetectionCommand { get; }
        public ICommand ApproveDetectionCommand { get; }
        public ICommand RejectDetectionCommand { get; }
        public ICommand ApproveAllCommand { get; }
        public ICommand DeleteManualRedactionCommand { get; }
        public ICommand PreviewRedactedCommand { get; }
        public ICommand SaveRedactedCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
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
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new RelayCommand(Cancel);
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

                // Check for existing saved detections
                await LoadDetectionsAsync();

                // If there are saved detections, skip the prompt
                if (Detections.Count > 0 || ManualRedactions.Count > 0)
                {
                    ShowDetectionPrompt = false;
                    Console.WriteLine($"[FileReview] Load complete, {Detections.Count} detections loaded");
                }
                else
                {
                    ShowDetectionPrompt = true;
                    Console.WriteLine($"[FileReview] Load complete, showing detection prompt");
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

            ShowDetectionPrompt = false;
            _hasUnsavedChanges = true;
            IsDetecting = true;
            ClearError();

            try
            {
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
                // Store detections locally only - will be saved on Save click
                foreach (var req in allDetections)
                {
                    var detection = new Detection
                    {
                        Id = "", // Empty ID indicates not saved to server yet
                        FileId = File.Id,
                        DetectionType = req.DetectionType,
                        TextContent = req.TextContent,
                        Confidence = req.Confidence,
                        BboxX = req.BboxX,
                        BboxY = req.BboxY,
                        BboxWidth = req.BboxWidth,
                        BboxHeight = req.BboxHeight,
                        PageNumber = req.PageNumber,
                        Status = "pending"
                    };
                    Console.WriteLine($"[Detection] Added local: bbox=({detection.BboxX:F3},{detection.BboxY:F3}), HasBbox: {detection.HasBoundingBox}");
                    Detections.Add(detection);
                }
                Console.WriteLine($"[Detection] Detections collection now has {Detections.Count} items");
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

        public Task ApproveDetectionAsync(Detection? detection)
        {
            if (detection == null) return Task.CompletedTask;
            // Update status locally only - will be saved on Save click
            detection.Status = "approved";
            _hasUnsavedChanges = true;
            return Task.CompletedTask;
        }

        public Task RejectDetectionAsync(Detection? detection)
        {
            if (detection == null) return Task.CompletedTask;
            // Remove from local collection - will not be saved
            Detections.Remove(detection);
            _hasUnsavedChanges = true;
            return Task.CompletedTask;
        }

        private Task ApproveAllAsync()
        {
            foreach (var detection in Detections.Where(d => d.Status == "pending").ToList())
            {
                detection.Status = "approved";
            }
            _hasUnsavedChanges = true;
            return Task.CompletedTask;
        }

        public Task AddManualRedaction(double x, double y, double width, double height)
        {
            if (File == null) return Task.CompletedTask;

            // Add locally only - will be saved on Save click
            var redaction = new ManualRedaction
            {
                Id = "", // Empty ID indicates not saved to server yet
                FileId = File.Id,
                BboxX = x,
                BboxY = y,
                BboxWidth = width,
                BboxHeight = height,
                PageNumber = File.IsPdf ? CurrentPage : null
            };
            ManualRedactions.Add(redaction);
            _hasUnsavedChanges = true;
            return Task.CompletedTask;
        }

        public Task DeleteManualRedactionAsync(ManualRedaction? redaction)
        {
            if (redaction == null) return Task.CompletedTask;
            // Remove locally only
            ManualRedactions.Remove(redaction);
            return Task.CompletedTask;
        }

        public Task UpdateDetectionAsync(Detection detection)
        {
            // Already updated in local object - nothing to do
            return Task.CompletedTask;
        }

        public Task UpdateManualRedactionAsync(ManualRedaction redaction)
        {
            // Already updated in local object - nothing to do
            return Task.CompletedTask;
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

        private async Task SaveAsync()
        {
            Console.WriteLine("[Save] SaveAsync called");
            if (File == null || _originalFileData == null)
            {
                Console.WriteLine("[Save] File or data is null, returning");
                return;
            }

            IsLoading = true;

            try
            {
                Console.WriteLine($"[Save] Total detections: {Detections.Count}");
                foreach (var d in Detections)
                {
                    Console.WriteLine($"[Save] Detection: status={d.Status}, bbox=({d.BboxX},{d.BboxY},{d.BboxWidth},{d.BboxHeight}), HasBbox={d.HasBoundingBox}");
                }

                Console.WriteLine("[Save] Clearing detections on server...");
                // Clear existing detections on server
                await _apiService.ClearDetectionsAsync(File.Id);

                // Save approved detections to API
                var approvedDetections = Detections
                    .Where(d => d.Status == "approved")
                    .Select(d => new CreateDetectionRequest
                    {
                        DetectionType = d.DetectionType,
                        BboxX = d.BboxX,
                        BboxY = d.BboxY,
                        BboxWidth = d.BboxWidth,
                        BboxHeight = d.BboxHeight,
                        PageNumber = d.PageNumber,
                        Status = "approved"
                    })
                    .ToList();

                Console.WriteLine($"[Save] Approved detections to save: {approvedDetections.Count}");
                if (approvedDetections.Count > 0)
                {
                    Console.WriteLine($"[Save] Creating detections via API...");
                    foreach (var req in approvedDetections)
                    {
                        Console.WriteLine($"[Save] Sending: bbox=({req.BboxX},{req.BboxY},{req.BboxWidth},{req.BboxHeight})");
                    }
                    var created = await _apiService.CreateDetectionsAsync(File.Id, approvedDetections);
                    Console.WriteLine($"[Save] Detections created ({created.Count}), updating status...");
                    foreach (var det in created)
                    {
                        Console.WriteLine($"[Save] Received: id={det.Id}, bbox=({det.BboxX},{det.BboxY},{det.BboxWidth},{det.BboxHeight})");
                    }

                    // API ignores status on create, so update each one
                    foreach (var det in created)
                    {
                        await _apiService.UpdateDetectionAsync(det.Id, new UpdateDetectionRequest { Status = "approved" });
                    }
                    Console.WriteLine($"[Save] Status updated");
                }

                // Save manual redactions to API
                foreach (var redaction in ManualRedactions)
                {
                    if (string.IsNullOrEmpty(redaction.Id))
                    {
                        await _apiService.CreateManualRedactionAsync(File.Id, new CreateManualRedactionRequest
                        {
                            BboxX = redaction.BboxX ?? 0,
                            BboxY = redaction.BboxY ?? 0,
                            BboxWidth = redaction.BboxWidth ?? 0,
                            BboxHeight = redaction.BboxHeight ?? 0,
                            PageNumber = redaction.PageNumber
                        });
                    }
                }

                // Generate and save redacted file
                byte[] redactedData;
                if (File.IsImage)
                {
                    redactedData = await _redactionService.RedactImageAsync(
                        _originalFileData,
                        Detections.Where(d => d.Status == "approved").ToList(),
                        ManualRedactions.ToList()
                    );
                }
                else
                {
                    redactedData = await _redactionService.RedactPdfAsync(
                        _originalFileData,
                        Detections.Where(d => d.Status == "approved").ToList(),
                        ManualRedactions.ToList()
                    );
                }

                var filename = File.IsPdf
                    ? Path.ChangeExtension(File.Filename, ".redacted.pdf")
                    : Path.ChangeExtension(File.Filename, ".redacted.jpg");

                Console.WriteLine($"[Save] Uploading redacted file: {filename}");
                try
                {
                    await _apiService.UploadRedactedFileAsync(File.Id, redactedData, filename);
                    Console.WriteLine("[Save] Redacted file uploaded");
                }
                catch (Exception uploadEx)
                {
                    // Log but don't fail - detections are saved
                    Console.WriteLine($"[Save] Redacted upload failed (non-fatal): {uploadEx.Message}");
                }

                Console.WriteLine("[Save] Complete, closing...");
                FileClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Save] ERROR: {ex.Message}");
                Console.WriteLine($"[Save] Stack: {ex.StackTrace}");
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            // Discard all local changes and close
            FileClosed?.Invoke(this, EventArgs.Empty);
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
