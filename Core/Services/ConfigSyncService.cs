using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Timer = System.Timers.Timer;

namespace SIEM_Agent.Core.Services
{
    public class ConfigSyncService
    {
        private readonly string _configUrl;
        private readonly string _localConfigPath;
        private readonly Timer _syncTimer;
        private readonly HttpClient _httpClient;

        public event EventHandler<string> OnConfigUpdated;
        public event EventHandler<Exception> OnSyncError;

        public ConfigSyncService(string configUrl, string localConfigPath)
        {
            _configUrl = configUrl;
            _localConfigPath = localConfigPath;
            _httpClient = new HttpClient();
            _syncTimer = new Timer(5 * 60 * 1000); // 5 phút
            _syncTimer.Elapsed += async (sender, e) => await SyncConfigAsync();
        }

        public void Start()
        {
            _syncTimer.Start();
            // Thực hiện đồng bộ ngay lập tức khi bắt đầu
            _ = SyncConfigAsync();
        }

        public void Stop()
        {
            _syncTimer.Stop();
        }

        private async Task SyncConfigAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_configUrl);
                response.EnsureSuccessStatusCode();

                var configContent = await response.Content.ReadAsStringAsync();
                var currentConfig = File.Exists(_localConfigPath) ? File.ReadAllText(_localConfigPath) : "";

                if (configContent != currentConfig)
                {
                    File.WriteAllText(_localConfigPath, configContent);
                    OnConfigUpdated?.Invoke(this, _localConfigPath);
                }
            }
            catch (Exception ex)
            {
                OnSyncError?.Invoke(this, ex);
            }
        }
    }
} 