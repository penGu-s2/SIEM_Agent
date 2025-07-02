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

                // Đọc cấu hình
                var configJson = File.ReadAllText("agent_config.json");
                var config = JsonSerializer.Deserialize<dynamic>(configJson);

                // Kiểm tra nếu config_sync.enable = true thì mới chạy đồng bộ
                if (config is System.Text.Json.JsonElement rootElem &&
                    rootElem.TryGetProperty("config_sync", out var syncCfg) &&
                    syncCfg.TryGetProperty("enable", out var enableProp) &&
                    enableProp.GetBoolean())
                {
                    var configSyncService = new ConfigSyncService(
                        syncCfg.GetProperty("url").GetString(),
                        "fluent-bit.conf"
                    );
                    configSyncService.OnConfigUpdated += (sender, configPath) =>
                    {
                        FluentBitHelper.RestartFluentBitWithNotify();
                    };
                    configSyncService.Start();
                }

                // Khởi động Fluent Bit ngay khi chạy app
                FluentBitHelper.RestartFluentBitWithNotify();

                // Khởi tạo repository và service
                var logRepository = new LogRepository("logs");
                var logManagementService = new LogManagementService(logRepository);

                // Chạy form chính
                var mainForm = new MainForm(logManagementService);
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động ứng dụng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}