using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;

namespace Redact1.ViewModels
{
    public partial class RequestDetailViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private RecordsRequest? _request;

        [ObservableProperty]
        private ObservableCollection<EvidenceFile> _files = new();

        [ObservableProperty]
        private ObservableCollection<Export> _exports = new();

        [ObservableProperty]
        private EvidenceFile? _selectedFile;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string _status = "new";

        [ObservableProperty]
        private bool _isUploading;

        public event EventHandler<EvidenceFile>? FileSelected;
        public event EventHandler? RequestClosed;

        public RequestDetailViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
        }

        public async Task LoadRequestAsync(string requestId)
        {
            IsLoading = true;
            ClearError();

            try
            {
                Request = await _apiService.GetRequestAsync(requestId);
                Title = Request.Title;
                Notes = Request.Notes ?? string.Empty;
                Status = Request.Status;

                await LoadFilesAsync();
                await LoadExportsAsync();
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
        private async Task LoadFilesAsync()
        {
            if (Request == null) return;

            try
            {
                var files = await _apiService.GetFilesAsync(Request.Id);
                Files.Clear();
                foreach (var file in files)
                {
                    Files.Add(file);
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private async Task LoadExportsAsync()
        {
            if (Request == null) return;

            try
            {
                var exports = await _apiService.GetExportsAsync(Request.Id);
                Exports.Clear();
                foreach (var export in exports)
                {
                    Exports.Add(export);
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private async Task SaveChangesAsync()
        {
            if (Request == null) return;

            try
            {
                var payload = new UpdateRequestPayload
                {
                    Title = Title,
                    Notes = Notes,
                    Status = Status
                };

                Request = await _apiService.UpdateRequestAsync(Request.Id, payload);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private async Task UploadFileAsync()
        {
            if (Request == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image and PDF files|*.jpg;*.jpeg;*.png;*.pdf|All files|*.*",
                Multiselect = true,
                Title = "Select files to upload"
            };

            if (dialog.ShowDialog() == true)
            {
                IsUploading = true;

                try
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        var file = await _apiService.UploadFileAsync(Request.Id, filePath);
                        Files.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    SetError(ex);
                }
                finally
                {
                    IsUploading = false;
                }
            }
        }

        [RelayCommand]
        private void OpenFile(EvidenceFile file)
        {
            SelectedFile = file;
            FileSelected?.Invoke(this, file);
        }

        [RelayCommand]
        private async Task DeleteFileAsync(EvidenceFile file)
        {
            try
            {
                await _apiService.DeleteFileAsync(file.Id);
                Files.Remove(file);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private async Task CreateExportAsync()
        {
            if (Request == null) return;

            IsLoading = true;

            try
            {
                var export = await _apiService.CreateExportAsync(Request.Id);
                Exports.Insert(0, export);
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
        private async Task DownloadExportAsync(Export export)
        {
            var dialog = new SaveFileDialog
            {
                FileName = export.Filename,
                Filter = "ZIP files|*.zip|All files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = await _apiService.DownloadExportAsync(export.Id);
                    await File.WriteAllBytesAsync(dialog.FileName, data);
                }
                catch (Exception ex)
                {
                    SetError(ex);
                }
            }
        }

        [RelayCommand]
        private void Close()
        {
            RequestClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
