using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class RequestDetailViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        private RecordsRequest? _request;
        private ObservableCollection<EvidenceFile> _files = new();
        private ObservableCollection<Export> _exports = new();
        private EvidenceFile? _selectedFile;
        private string _title = string.Empty;
        private string _notes = string.Empty;
        private string _status = "new";
        private bool _isUploading;
        private bool _isConfirmingDelete;
        private bool _isConfirmingFileDelete;
        private EvidenceFile? _fileToDelete;

        public RecordsRequest? Request
        {
            get => _request;
            set => SetProperty(ref _request, value);
        }

        public ObservableCollection<EvidenceFile> Files
        {
            get => _files;
            set => SetProperty(ref _files, value);
        }

        public ObservableCollection<Export> Exports
        {
            get => _exports;
            set => SetProperty(ref _exports, value);
        }

        public EvidenceFile? SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        public bool IsConfirmingFileDelete
        {
            get => _isConfirmingFileDelete;
            set => SetProperty(ref _isConfirmingFileDelete, value);
        }

        public EvidenceFile? FileToDelete
        {
            get => _fileToDelete;
            set => SetProperty(ref _fileToDelete, value);
        }

        public ICommand LoadFilesCommand { get; }
        public ICommand LoadExportsCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand UploadFileCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public ICommand RequestDeleteFileCommand { get; }
        public ICommand ConfirmDeleteFileCommand { get; }
        public ICommand CancelDeleteFileCommand { get; }
        public ICommand CreateExportCommand { get; }
        public ICommand DownloadExportCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ArchiveRequestCommand { get; }
        public ICommand RequestDeleteCommand { get; }
        public ICommand ConfirmDeleteRequestCommand { get; }
        public ICommand CancelDeleteRequestCommand { get; }

        public event EventHandler<EvidenceFile>? FileSelected;
        public event EventHandler? RequestClosed;
        public event EventHandler? RequestArchived;
        public event EventHandler? RequestDeleted;

        public RequestDetailViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();

            LoadFilesCommand = new AsyncRelayCommand(LoadFilesAsync);
            LoadExportsCommand = new AsyncRelayCommand(LoadExportsAsync);
            SaveChangesCommand = new AsyncRelayCommand(SaveChangesAsync);
            UploadFileCommand = new AsyncRelayCommand(UploadFileAsync);
            OpenFileCommand = new RelayCommand<EvidenceFile>(OpenFile);
            DeleteFileCommand = new AsyncRelayCommand<EvidenceFile>(DeleteFileAsync);
            RequestDeleteFileCommand = new RelayCommand<EvidenceFile>(RequestDeleteFile);
            ConfirmDeleteFileCommand = new AsyncRelayCommand(ConfirmDeleteFileAsync);
            CancelDeleteFileCommand = new RelayCommand(CancelDeleteFile);
            CreateExportCommand = new AsyncRelayCommand(CreateExportAsync);
            DownloadExportCommand = new AsyncRelayCommand<Export>(DownloadExportAsync);
            CloseCommand = new RelayCommand(Close);
            ArchiveRequestCommand = new AsyncRelayCommand(ArchiveRequestAsync);
            RequestDeleteCommand = new RelayCommand(RequestDelete);
            ConfirmDeleteRequestCommand = new AsyncRelayCommand(ConfirmDeleteRequestAsync);
            CancelDeleteRequestCommand = new RelayCommand(CancelDeleteRequest);
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

        private async Task UploadFileAsync()
        {
            // File upload would need platform-specific file picker
            // This is a placeholder - in Avalonia you'd use IStorageProvider
            await Task.CompletedTask;
        }

        private void OpenFile(EvidenceFile? file)
        {
            if (file == null) return;
            SelectedFile = file;
            FileSelected?.Invoke(this, file);
        }

        private async Task DeleteFileAsync(EvidenceFile? file)
        {
            if (file == null) return;

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

        private void RequestDeleteFile(EvidenceFile? file)
        {
            if (file == null) return;
            FileToDelete = file;
            IsConfirmingFileDelete = true;
        }

        private async Task ConfirmDeleteFileAsync()
        {
            if (FileToDelete == null) return;

            try
            {
                await _apiService.DeleteFileAsync(FileToDelete.Id);
                Files.Remove(FileToDelete);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsConfirmingFileDelete = false;
                FileToDelete = null;
            }
        }

        private void CancelDeleteFile()
        {
            IsConfirmingFileDelete = false;
            FileToDelete = null;
        }

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

        private async Task DownloadExportAsync(Export? export)
        {
            // Download would need platform-specific file save dialog
            // This is a placeholder
            await Task.CompletedTask;
        }

        private void Close()
        {
            RequestClosed?.Invoke(this, EventArgs.Empty);
        }

        private async Task ArchiveRequestAsync()
        {
            if (Request == null) return;

            try
            {
                await _apiService.ArchiveRequestAsync(Request.Id);
                RequestArchived?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private void RequestDelete()
        {
            IsConfirmingDelete = true;
        }

        private async Task ConfirmDeleteRequestAsync()
        {
            if (Request == null) return;

            try
            {
                await _apiService.DeleteRequestAsync(Request.Id);
                IsConfirmingDelete = false;
                RequestDeleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetError(ex);
                IsConfirmingDelete = false;
            }
        }

        private void CancelDeleteRequest()
        {
            IsConfirmingDelete = false;
        }
    }
}
