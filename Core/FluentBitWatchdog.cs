using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SIEM_Agent.Core; // Để sử dụng FluentBitHelper

namespace SIEM_Agent.Core
{
    public class FluentBitWatchdog : IDisposable
    {
        private CancellationTokenSource _cts;
        private Task _watchdogTask;
        private const int CHECK_INTERVAL_SECONDS = 10; // Kiểm tra mỗi 10 giây
        private const int RESTART_DEBOUNCE_SECONDS = 30; // Chờ 30 giây giữa các lần restart
        private DateTime _lastRestartTime = DateTime.MinValue;

        private static string mainConf = "fluent-bit.conf";
        private static string backupConf = "fluent-bit2.conf";
        private static string agentConfig = "agent_config.json";

        // Biến lưu config toàn cục và thời gian reload
        private AgentConfig _globalConfig;
        private DateTime _lastConfigReload = DateTime.MinValue;
        private const int CONFIG_RELOAD_INTERVAL_SECONDS = 300; // 5 phút mặc định

        public FluentBitWatchdog()
        {
            // Đảm bảo thư mục logs tồn tại
            if (!Directory.Exists("logs"))
            {
                Directory.CreateDirectory("logs");
            }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _watchdogTask = Task.Run(() => RunWatchdog(_cts.Token));
            LogWatchdogAction("FluentBitWatchdog đã khởi động.");
        }

        public void Stop()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                try
                {
                    _watchdogTask?.Wait(5000); // Chờ task dừng tối đa 5 giây
                }
                catch (OperationCanceledException) { /* Task đã bị hủy */ }
                catch (Exception ex)
                {
                    LogWatchdogError($"Lỗi khi dừng watchdog task: {ex.Message}");
                }
            }
            LogWatchdogAction("FluentBitWatchdog đã dừng.");
        }

        private async Task RunWatchdog(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ReloadConfigIfNeeded(); // Reload config định kỳ

                    // Kiểm tra cấu hình Fluent Bit trước
                    var configStatus = ValidateFluentBitConfig();
                    if (configStatus.HasError)
                    {
                        LogWatchdogError($"Phát hiện lỗi cấu hình Fluent Bit: {configStatus.ErrorMessage}");
                        await SendErrorLog(configStatus.ErrorMessage, configStatus.ErrorType, mainConf);
                        
                        // Thử khôi phục từ backup nếu có lỗi
                        if (configStatus.ErrorType == "EmptyConfig" || configStatus.ErrorType == "InvalidConfig")
                        {
                            await TryRestoreFromBackup();
                        }
                    }

                    if (!FluentBitHelper.IsFluentBitRunning())
                    {
                        LogWatchdogAction("Fluent Bit không chạy. Đang kiểm tra và khởi động lại...");

                        // Kiểm tra debounce để tránh restart liên tục
                        if ((DateTime.Now - _lastRestartTime).TotalSeconds < RESTART_DEBOUNCE_SECONDS)
                        {
                            LogWatchdogAction($"Đang trong thời gian debounce. Chờ thêm {RESTART_DEBOUNCE_SECONDS - (DateTime.Now - _lastRestartTime).TotalSeconds:F0} giây.");
                            await Task.Delay(CHECK_INTERVAL_SECONDS * 1000, cancellationToken); // Chờ lâu hơn trong debounce
                            continue;
                        }

                        // Luôn thử khởi động lại Fluent Bit sau khi kiểm tra/khôi phục config
                        try
                        {
                            FluentBitHelper.RestartFluentBitWithNotify();
                            _lastRestartTime = DateTime.Now;
                            LogWatchdogAction("Đã gọi FluentBitHelper.RestartFluentBitWithNotify().");
                        }
                        catch (Exception ex)
                        {
                            LogWatchdogError($"Lỗi khi khởi động lại Fluent Bit: {ex.Message}");
                            await SendErrorLog($"Lỗi khi khởi động lại Fluent Bit: {ex.Message}", "FluentBitRestartError", mainConf);
                        }
                    }
                    else
                    {
                        // Kiểm tra xem Fluent Bit có thực sự hoạt động không
                        var healthCheck = await CheckFluentBitHealth();
                        if (!healthCheck.IsHealthy)
                        {
                            LogWatchdogError($"Fluent Bit đang chạy nhưng không khỏe mạnh: {healthCheck.ErrorMessage}");
                            await SendErrorLog($"Fluent Bit không khỏe mạnh: {healthCheck.ErrorMessage}", "FluentBitUnhealthy", mainConf);
                        }
                        else
                        {
                            LogWatchdogAction("Fluent Bit đang chạy ổn định và khỏe mạnh.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task bị hủy, thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    LogWatchdogError($"Lỗi không xác định trong watchdog: {ex.Message}");
                    await SendErrorLog($"Lỗi không xác định trong watchdog: {ex.Message}", "WatchdogRuntimeError", "N/A");
                }

                await Task.Delay(CHECK_INTERVAL_SECONDS * 1000, cancellationToken);
            }
        }



        private async Task<FluentBitHealthResult> CheckFluentBitHealth()
        {
            try
            {
                // Kiểm tra xem Fluent Bit có đang ghi logs không
                var logFile = Path.Combine("logs", "fluentbit_output.txt");
                if (File.Exists(logFile))
                {
                    var fileInfo = new FileInfo(logFile);
                    var timeSinceLastWrite = DateTime.Now - fileInfo.LastWriteTime;
                    
                    // Nếu file không được cập nhật trong 5 phút, coi như không khỏe mạnh
                    if (timeSinceLastWrite.TotalMinutes > 5)
                    {
                        return new FluentBitHealthResult
                        {
                            IsHealthy = false,
                            ErrorMessage = $"File log không được cập nhật trong {timeSinceLastWrite.TotalMinutes:F0} phút - Fluent Bit có thể bị treo"
                        };
                    }
                }

                return new FluentBitHealthResult { IsHealthy = true };
            }
            catch (Exception ex)
            {
                return new FluentBitHealthResult
                {
                    IsHealthy = false,
                    ErrorMessage = $"Lỗi khi kiểm tra sức khỏe Fluent Bit: {ex.Message}"
                };
            }
        }

        private async Task TryRestoreFromBackup()
        {
            try
            {
                if (File.Exists(backupConf))
                {
                    LogWatchdogAction($"Đang khôi phục cấu hình từ file backup '{backupConf}'...");
                    File.Copy(backupConf, mainConf, true);
                    LogWatchdogAction($"Đã khôi phục cấu hình từ '{backupConf}' sang '{mainConf}'.");
                    await SendErrorLog($"Đã khôi phục cấu hình Fluent Bit từ file backup.", "ConfigRestored", mainConf);
                }
                else
                {
                    LogWatchdogError($"Không tìm thấy file backup '{backupConf}' để khôi phục.");
                    await SendErrorLog($"Không tìm thấy file backup '{backupConf}' để khôi phục.", "NoBackupFound", mainConf);
                }
            }
            catch (Exception ex)
            {
                LogWatchdogError($"Lỗi khi khôi phục cấu hình từ backup: {ex.Message}");
                await SendErrorLog($"Lỗi khi khôi phục cấu hình từ backup: {ex.Message}", "ConfigRestoreError", mainConf);
            }
        }

        private ConfigValidationResult ValidateFluentBitConfig()
        {
            try
            {
                if (!File.Exists(mainConf))
                {
                    return new ConfigValidationResult
                    {
                        HasError = true,
                        ErrorType = "FileNotFound",
                        ErrorMessage = $"File cấu hình '{mainConf}' không tồn tại"
                    };
                }

                var content = File.ReadAllText(mainConf);
                
                // Kiểm tra file trống
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new ConfigValidationResult
                    {
                        HasError = true,
                        ErrorType = "EmptyConfig",
                        ErrorMessage = $"File cấu hình '{mainConf}' trống - Fluent Bit sẽ chạy với cấu hình mặc định (không thu thập logs)"
                    };
                }

                // Kiểm tra cấu trúc cơ bản
                if (content.Trim().Length < 10)
                {
                    return new ConfigValidationResult
                    {
                        HasError = true,
                        ErrorType = "InvalidConfig",
                        ErrorMessage = $"File cấu hình '{mainConf}' quá ngắn - có thể không hợp lệ"
                    };
                }

                if (!content.Contains("[") || !content.Contains("]"))
                {
                    return new ConfigValidationResult
                    {
                        HasError = true,
                        ErrorType = "InvalidConfig",
                        ErrorMessage = $"File cấu hình '{mainConf}' thiếu sections - cần có ít nhất một section [INPUT] hoặc [OUTPUT]"
                    };
                }

                // Kiểm tra có ít nhất một INPUT và OUTPUT
                if (!content.Contains("[INPUT]") && !content.Contains("[OUTPUT]"))
                {
                    return new ConfigValidationResult
                    {
                        HasError = true,
                        ErrorType = "InvalidConfig",
                        ErrorMessage = $"File cấu hình '{mainConf}' thiếu INPUT/OUTPUT - Fluent Bit sẽ không thu thập logs"
                    };
                }

                return new ConfigValidationResult { HasError = false };
            }
            catch (Exception ex)
            {
                return new ConfigValidationResult
                {
                    HasError = true,
                    ErrorType = "ConfigReadError",
                    ErrorMessage = $"Lỗi khi đọc file cấu hình '{mainConf}': {ex.Message}"
                };
            }
        }

        private void ReloadConfigIfNeeded()
        {
            if ((DateTime.Now - _lastConfigReload).TotalSeconds > CONFIG_RELOAD_INTERVAL_SECONDS || _globalConfig == null)
            {
                try
                {
                    // Reload agent_config.json (cấu hình agent, không phải fluent-bit.conf)
                    _globalConfig = JsonSerializer.Deserialize<AgentConfig>(File.ReadAllText(agentConfig));
                    _lastConfigReload = DateTime.Now;

                    // Nếu có cấu hình khoảng thời gian trong file, dùng nó (tối thiểu 60 giây)
                    // int minutes = _globalConfig?.config_sync?.interval_minutes > 0 ? _globalConfig.config_sync.interval_minutes : 5;
                    // CONFIG_RELOAD_INTERVAL_SECONDS = Math.Max(60, minutes * 60);

                    LogWatchdogAction($"Đã reload lại agent config lúc {_lastConfigReload:HH:mm:ss dd/MM/yyyy}, interval = {CONFIG_RELOAD_INTERVAL_SECONDS}s");
                }
                catch (Exception ex)
                {
                    LogWatchdogError("Lỗi reload agent config: " + ex.Message);
                }
            }
        }

        private async Task SendErrorLog(string errorMsg, string errorType, string confFile)
        {
            try
            {
                var config = _globalConfig;
                var agent = config?.agent_info;
                var serverUrl = config?.config_sync?.url ?? "http://localhost:5000/log";
                var token = config?.config_sync?.token ?? "";
                string localIP = GetLocalIPAddress();

                using var client = new HttpClient();
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var log = new
                {
                    time = DateTime.UtcNow,
                    agent_id = agent?.agent_id ?? "unknown",
                    agent_name = agent?.name ?? "unknown",
                    agent_description = agent?.description ?? "",
                    ip = localIP,
                    error_type = errorType,
                    conf_file = confFile,
                    error_detail = errorMsg
                };
                var content = new StringContent(JsonSerializer.Serialize(log), System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(serverUrl, content);
            }
            catch (Exception ex)
            {
                LogWatchdogError("Gửi log lỗi thất bại: " + ex.Message);
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "Không xác định";
            }
            catch
            {
                return "Không xác định";
            }
        }

        private void LogWatchdogAction(string message)
        {
            string logPath = Path.Combine("logs", "watchdog.log");
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi ghi log watchdog: {ex.Message} - {message}");
            }
        }

        private void LogWatchdogError(string message)
        {
            string logPath = Path.Combine("logs", "watchdog.log");
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi ghi log watchdog: {ex.Message} - {message}");
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _watchdogTask?.Dispose();
        }

        // Các class kết quả kiểm tra
        private class ConfigValidationResult
        {
            public bool HasError { get; set; }
            public string ErrorType { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class FluentBitHealthResult
        {
            public bool IsHealthy { get; set; }
            public string ErrorMessage { get; set; }
        }

        // Định nghĩa các class cấu hình nội bộ
        private class AgentConfig
        {
            public ConfigSync config_sync { get; set; }
            public AgentInfo agent_info { get; set; }
            public class ConfigSync
            {
                public bool enabled { get; set; }
                public string url { get; set; }
                public int interval_minutes { get; set; }
                public string token { get; set; }
            }
            public class AgentInfo
            {
                public string agent_id { get; set; }
                public string name { get; set; }
                public string description { get; set; }
                public long created_time { get; set; }
                public long updated_time { get; set; }
            }
        }
    }
}