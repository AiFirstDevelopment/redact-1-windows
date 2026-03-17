using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;

namespace Redact1.ViewModels
{
    public partial class RequestsViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<RecordsRequest> _requests = new();

        [ObservableProperty]
        private RecordsRequest? _selectedRequest;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusFilter = "all";

        [ObservableProperty]
        private bool _showArchived;

        public event EventHandler<RecordsRequest>? RequestSelected;

        public RequestsViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
        }

        [RelayCommand]
        public async Task LoadRequestsAsync()
        {
            IsLoading = true;
            ClearError();

            try
            {
                List<RecordsRequest> requests;

                if (ShowArchived)
                {
                    requests = await _apiService.GetArchivedRequestsAsync(
                        string.IsNullOrWhiteSpace(SearchText) ? null : SearchText
                    );
                }
                else
                {
                    var status = StatusFilter == "all" ? null : StatusFilter;
                    requests = await _apiService.GetRequestsAsync(
                        status,
                        string.IsNullOrWhiteSpace(SearchText) ? null : SearchText
                    );
                }

                Requests.Clear();
                foreach (var request in requests)
                {
                    Requests.Add(request);
                }
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
        private async Task CreateRequestAsync()
        {
            // This would open a dialog in the View
            var requestNumber = $"FOIA-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            var payload = new CreateRequestPayload
            {
                RequestNumber = requestNumber,
                Title = "New Request",
                RequestDate = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            try
            {
                var request = await _apiService.CreateRequestAsync(payload);
                Requests.Insert(0, request);
                SelectedRequest = request;
                RequestSelected?.Invoke(this, request);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private void OpenRequest(RecordsRequest request)
        {
            SelectedRequest = request;
            RequestSelected?.Invoke(this, request);
        }

        [RelayCommand]
        private async Task ArchiveRequestAsync(RecordsRequest request)
        {
            try
            {
                await _apiService.ArchiveRequestAsync(request.Id);
                Requests.Remove(request);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private async Task UnarchiveRequestAsync(RecordsRequest request)
        {
            try
            {
                await _apiService.UnarchiveRequestAsync(request.Id);
                Requests.Remove(request);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        [RelayCommand]
        private async Task DeleteRequestAsync(RecordsRequest request)
        {
            try
            {
                await _apiService.DeleteRequestAsync(request.Id);
                Requests.Remove(request);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = LoadRequestsAsync();
        }

        partial void OnStatusFilterChanged(string value)
        {
            _ = LoadRequestsAsync();
        }
    }
}
