using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace Redact1.ViewModels
{
    public partial class FileReviewViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IDetectionService _detectionService;
        private readonly IRedactionService _redactionService;

        private byte[]? _originalFileData;
        private byte[]? _redactedFileData;

        [ObservableProperty]
        private EvidenceFile? _file;

        [ObservableProperty]
        private ObservableCollection<Detection> _detections = new();

        [ObservableProperty]
        private ObservableCollection<ManualRedaction> _manualRedactions = new();

        [ObservableProperty]
        private Detection? _selectedDetection;

        [ObservableProperty]
        private BitmapImage? _displayImage;

        [ObservableProperty]
        private BitmapImage? _redactedImage;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private bool _isDetecting;

        [ObservableProperty]
        private bool _showRedacted;

        [ObservableProperty]
        private bool _isDrawingMode;

        public event EventHandler? FileClosed;

        public FileReviewViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
            _detectionService = App.Services.GetRequiredService<IDetectionService>();
            _redactionService = App.Services.GetRequiredService<IRedactionService>();
        }

        public async Task LoadFileAsync(string fileId)
        {
            IsLoading = true;
            ClearError();

            try
            {
                File = await _apiService.GetFileAsync(fileId);
                _originalFileData = await _apiService.GetOriginalFileAsync(fileId);

                if (File.IsPdf)
                {
                    TotalPages = _redactionService.GetPdfPageCount(_originalFileData);
                    await LoadPdfPageAsync(1);
                }
                else
                {
                    await LoadImageAsync();
                }

                await LoadDetectionsAsync();
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

        private async Task LoadImageAsync()
        {
            if (_originalFileData == null) return;

            await Task.Run(() =>
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(_originalFileData);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DisplayImage = image;
                });
            });
        }

        private async Task LoadPdfPageAsync(int pageNumber)
        {
            if (_originalFileData == null) return;

            var pageImage = await _redactionService.RenderPdfPageToImageAsync(_originalFileData, pageNumber);

            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(pageImage);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            DisplayImage = image;
            CurrentPage = pageNumber;
        }

        [RelayCommand]
        private async Task LoadDetectionsAsync()
        {
            if (File == null) return;

            try
            {
                var result = await _apiService.GetDetectionsAsync(File.Id);

                Detections.Clear();
                foreach (var detection in result.Detections)
                {
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

        [RelayCommand]
        private async Task RunDetectionAsync()
        {
            if (File == null || _originalFileData == null) return;

            IsDetecting = true;
            ClearError();

            try
            {
                // Clear existing detections
                await _apiService.ClearDetectionsAsync(File.Id);
                Detections.Clear();

                List<CreateDetectionRequest> allDetections = new();

                if (File.IsImage)
                {
                    var detected = await _detectionService.DetectInImageAsync(_originalFileData);
                    allDetections.AddRange(detected);
                }
                else if (File.IsPdf)
                {
                    for (int page = 1; page <= TotalPages; page++)
                    {
                        var pageImage = await _redactionService.RenderPdfPageToImageAsync(_originalFileData, page);
                        var detected = await _detectionService.DetectInPdfPageAsync(pageImage, page);
                        allDetections.AddRange(detected);
                    }
                }

                if (allDetections.Count > 0)
                {
                    var created = await _apiService.CreateDetectionsAsync(File.Id, allDetections);
                    foreach (var detection in created)
                    {
                        Detections.Add(detection);
                    }
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsDetecting = false;
            }
        }

        [RelayCommand]
        private async Task ApproveDetectionAsync(Detection detection)
        {
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

        [RelayCommand]
        private async Task RejectDetectionAsync(Detection detection)
        {
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

        [RelayCommand]
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

        [RelayCommand]
        private async Task DeleteManualRedactionAsync(ManualRedaction redaction)
        {
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

        [RelayCommand]
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
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(redactedData);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    RedactedImage = image;
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

        [RelayCommand]
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

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                await LoadPdfPageAsync(CurrentPage + 1);
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                await LoadPdfPageAsync(CurrentPage - 1);
            }
        }

        [RelayCommand]
        private void ToggleDrawingMode()
        {
            IsDrawingMode = !IsDrawingMode;
        }

        [RelayCommand]
        private void Close()
        {
            FileClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
