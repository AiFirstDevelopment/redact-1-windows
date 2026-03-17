using System.IO;
using System.Text.Json;
using Redact1.Models;

namespace Redact1.Services
{
    public class StorageService : IStorageService
    {
        private readonly string _storagePath;
        private Dictionary<string, string> _storage = new();

        public StorageService()
        {
            _storagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Redact1",
                "storage.json"
            );
            LoadStorage();
        }

        private void LoadStorage()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    _storage = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
            }
            catch
            {
                _storage = new Dictionary<string, string>();
            }
        }

        private void SaveStorage()
        {
            try
            {
                var directory = Path.GetDirectoryName(_storagePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var json = JsonSerializer.Serialize(_storage);
                File.WriteAllText(_storagePath, json);
            }
            catch
            {
                // Silently fail on write errors
            }
        }

        private string? GetValue(string key)
        {
            return _storage.TryGetValue(key, out var value) ? value : null;
        }

        private void SetValue(string key, string? value)
        {
            if (value == null)
            {
                _storage.Remove(key);
            }
            else
            {
                _storage[key] = value;
            }
            SaveStorage();
        }

        public string? GetAuthToken()
        {
            return GetValue(App.Settings.StorageKeys.AuthToken);
        }

        public void SetAuthToken(string? token)
        {
            SetValue(App.Settings.StorageKeys.AuthToken, token);
        }

        public void ClearAuthToken()
        {
            SetValue(App.Settings.StorageKeys.AuthToken, null);
        }

        public User? GetUser()
        {
            var json = GetValue(App.Settings.StorageKeys.User);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<User>(json);
            }
            catch
            {
                return null;
            }
        }

        public void SetUser(User? user)
        {
            if (user == null)
            {
                SetValue(App.Settings.StorageKeys.User, null);
            }
            else
            {
                SetValue(App.Settings.StorageKeys.User, JsonSerializer.Serialize(user));
            }
        }

        public void ClearUser()
        {
            SetValue(App.Settings.StorageKeys.User, null);
        }

        public AgencyConfig? GetAgencyConfig()
        {
            var json = GetValue(App.Settings.StorageKeys.AgencyConfig);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<AgencyConfig>(json);
            }
            catch
            {
                return null;
            }
        }

        public void SetAgencyConfig(AgencyConfig? config)
        {
            if (config == null)
            {
                SetValue(App.Settings.StorageKeys.AgencyConfig, null);
            }
            else
            {
                SetValue(App.Settings.StorageKeys.AgencyConfig, JsonSerializer.Serialize(config));
            }
        }

        public void ClearAgencyConfig()
        {
            SetValue(App.Settings.StorageKeys.AgencyConfig, null);
        }

        public void ClearAll()
        {
            _storage.Clear();
            SaveStorage();
        }
    }
}
