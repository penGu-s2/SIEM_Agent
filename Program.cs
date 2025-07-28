using System;
using System.Windows.Forms;
using SIEM_Agent.UI.Forms;
using SIEM_Agent.Core.Services;
using SIEM_Agent.Core.Repositories;
using SIEM_Agent.Core;
using SIEM_Agent.UI;
using System.Text.Json;
using System.IO;

namespace SIEM_Agent
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Tạo thư mục logs nếu chưa tồn tại
                if (!System.IO.Directory.Exists("logs"))
                {
                    System.IO.Directory.CreateDirectory("logs");
                }

                // Kiểm tra file agent_config.json tồn tại
                if (!File.Exists("agent_config.json"))
                {
                    MessageBox.Show("Không tìm thấy file agent_config.json!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Đọc cấu hình
                var configJson = File.ReadAllText("agent_config.json");
                var config = JsonSerializer.Deserialize<dynamic>(configJson);

                // Luôn khởi động Fluent Bit khi chạy app
                FluentBitHelper.RestartFluentBitWithNotify();

                if (config is System.Text.Json.JsonElement rootElem &&
                    rootElem.TryGetProperty("config_sync", out var syncCfg) &&
                    syncCfg.TryGetProperty("enabled", out var enableProp) &&
                    enableProp.GetBoolean())
                {
                    var configSyncService = new ConfigSyncService(
                        syncCfg.GetProperty("url").GetString(),
                        "fluent-bit.conf"
                    );
                    configSyncService.OnConfigUpdated += (sender, configPath) =>
                    {
                        string logPath = Path.Combine("logs", "config_changes.log");
                        using (var sw = new StreamWriter(logPath, true))
                        {
                            sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Đã cập nhật fluent-bit.conf");
                        }
                        // Đảm bảo dừng fluent-bit cũ trước khi restart lại
                        FluentBitHelper.StopFluentBit();
                        FluentBitHelper.RestartFluentBitWithNotify();
                    };
                    configSyncService.Start();
                }

                // Khởi tạo repository và service
                var logRepository = new LogRepository("logs");
                var logManagementService = new LogManagementService(logRepository);

                // Chạy WebViewForm thay vì MainForm
                var webViewForm = new WebViewForm(logManagementService);
                Application.Run(webViewForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động ứng dụng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}