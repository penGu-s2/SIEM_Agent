using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using SIEM_Agent.Core.Services;
using SIEM_Agent.Core;
using SIEM_Agent.Core;

namespace SIEM_Agent.UI.Forms
{
    public class WebViewForm : Form
    {
        private WebView2 webView2;
        private readonly LogManagementService _logManagementService;
        private FluentBitWatchdog _watchdog;

        public WebViewForm()
        {
            Console.WriteLine("🚀 WebViewForm constructor được gọi");
            this.Text = "SIEM Agent";
            this.Width = 1200;
            this.Height = 800;
            this.MinimumSize = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // Chỉ còn WebView2 chiếm toàn bộ form
            webView2 = new WebView2();
            webView2.Dock = DockStyle.Fill;
            this.Controls.Add(webView2);
            webView2.BringToFront();

            // Mặc định load Dashboard
            Console.WriteLine("🔄 Bắt đầu load dashboard HTML");
            LoadDashboardHtmlFile();

            // Lắng nghe message từ web (toggle collector)
            webView2.CoreWebView2InitializationCompleted += (s, e) =>
            {
                webView2.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;
            };

            // Khởi động watchdog tối giản
            _watchdog = new FluentBitWatchdog();
            _watchdog.Start();
        }

        public WebViewForm(LogManagementService logManagementService) : this()
        {
            _logManagementService = logManagementService ?? throw new ArgumentNullException(nameof(logManagementService));
            
            // Xử lý đóng form
            this.FormClosing += WebViewForm_FormClosing;
        }

        private void WebViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                FluentBitHelper.StopFluentBit();
                _watchdog?.Stop();
                _watchdog?.Dispose();
            }
            catch { }
        }

        private void LoadDashboardHtmlFile()
        {
            Console.WriteLine("📁 LoadDashboardHtmlFile được gọi");
            // Đường dẫn tới file dashboard.html trong thư mục build/output (web folder riêng)
            string htmlPath = Path.Combine(Application.StartupPath, "UI/Forms/WebForm/web/dashboard.html");
            Console.WriteLine($"📁 Đường dẫn HTML: {htmlPath}");
            Console.WriteLine($"📁 File tồn tại: {File.Exists(htmlPath)}");
            
            if (!File.Exists(htmlPath))
            {
                Console.WriteLine("❌ Không tìm thấy file dashboard.html");
                MessageBox.Show($"Không tìm thấy file dashboard.html ở: {htmlPath}\n\nHãy kiểm tra lại thuộc tính Copy to Output Directory của file HTML/CSS/JS!", "Lỗi file không tồn tại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            Console.WriteLine("✅ File dashboard.html tồn tại, đang load vào WebView2");
            webView2.Source = new Uri(htmlPath);
            webView2.NavigationCompleted += WebView2_NavigationCompleted;
            Console.WriteLine("✅ Đã đăng ký NavigationCompleted event");
        }

        private async void WebView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            // Đảm bảo JS đã sẵn sàng trước khi gửi dữ liệu collectors
            var ready = await WaitForJsReady(5000);
            if (!ready)
            {
                Console.WriteLine("❌ JS chưa sẵn sàng sau 5s, vẫn thử gửi collectors (best-effort)");
            }

            // Sử dụng lại logic của ReloadAndSendCollectors để đảm bảo đồng nhất collector
            await ReloadAndSendCollectors();
        }

        private async System.Threading.Tasks.Task<bool> WaitForJsReady(int timeoutMs = 5000)
        {
            try
            {
                var start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
                {
                    var result = await webView2.CoreWebView2.ExecuteScriptAsync("typeof window.updateCollectorsFromCSharp === 'function'");
                    if (result != null && result.Trim('"').Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("✅ JS ready: updateCollectorsFromCSharp đã sẵn sàng");
                        return true;
                    }
                    await System.Threading.Tasks.Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi WaitForJsReady: {ex.Message}");
            }
            return false;
        }

        private async void WebView2_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            Console.WriteLine("Đã nhận message: " + e.WebMessageAsJson);
            try
            {
                var msg = JsonSerializer.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (msg != null && msg.ContainsKey("action"))
                {
                    if (msg["action"].ToString() == "toggle")
                    {
                        string tag = msg["tag"].ToString();
                        bool enable = false;
                        if (msg["enable"] is System.Text.Json.JsonElement je)
                            enable = je.GetBoolean();
                        else
                            enable = Convert.ToBoolean(msg["enable"]);
                        UpdateOutputBlockByTag(tag, enable);
                        await ReloadAndSendCollectors();
                    }
                    else if (msg["action"].ToString() == "add_collector")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dataElem.GetRawText());
                            if (data != null && data.ContainsKey("type"))
                            {
                                AddInputBlockDynamic(data);
                                await ReloadAndSendCollectors();
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "add_collector_with_output")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElem.GetRawText());
                            if (data != null)
                            {
                                AddCollectorWithOutput(data);
                                await ReloadAndSendCollectors();
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "get_parsers")
                    {
                        var parsers = ReadParsersFromFile();
                        string json = JsonSerializer.Serialize(parsers);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"updateParsersFromCSharp({JsonSerializer.Serialize(json)})");
                    }
                    else if (msg["action"].ToString() == "add_parser")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dataElem.GetRawText());
                            if (data != null)
                            {
                                AddParserBlock(data);
                                var parsers = ReadParsersFromFile();
                                string json = JsonSerializer.Serialize(parsers);
                                await webView2.CoreWebView2.ExecuteScriptAsync($"updateParsersFromCSharp({JsonSerializer.Serialize(json)})");
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "edit_parser")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem && msg.ContainsKey("idx"))
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dataElem.GetRawText());
                            int idx = -1;
                            if (msg["idx"] is System.Text.Json.JsonElement idxElem)
                            {
                                if (idxElem.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    idx = idxElem.GetInt32();
                                else if (idxElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                    int.TryParse(idxElem.GetString(), out idx);
                            }
                            if (data != null)
                            {
                                EditParserBlock(idx, data);
                                var parsers = ReadParsersFromFile();
                                string json = JsonSerializer.Serialize(parsers);
                                await webView2.CoreWebView2.ExecuteScriptAsync($"updateParsersFromCSharp({JsonSerializer.Serialize(json)})");
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "delete_parser")
                    {
                        if (msg.ContainsKey("idx"))
                        {
                            int idx = Convert.ToInt32(msg["idx"]);
                            DeleteParserBlock(idx);
                            var parsers = ReadParsersFromFile();
                            string json = JsonSerializer.Serialize(parsers);
                            await webView2.CoreWebView2.ExecuteScriptAsync($"updateParsersFromCSharp({JsonSerializer.Serialize(json)})");
                        }
                    }
                    else if (msg["action"].ToString() == "get_parser_by_idx")
                    {
                        if (msg.ContainsKey("idx"))
                        {
                            int idx = -1;
                            if (msg["idx"] is System.Text.Json.JsonElement idxElem)
                            {
                                if (idxElem.ValueKind == System.Text.Json.JsonValueKind.Number)
                                    idx = idxElem.GetInt32();
                                else if (idxElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                    int.TryParse(idxElem.GetString(), out idx);
                            }
                            var parsers = ReadParsersFromFile();
                            if (idx >= 0 && idx < parsers.Count)
                            {
                                string json = JsonSerializer.Serialize(parsers[idx]);
                                await webView2.CoreWebView2.ExecuteScriptAsync($"window.openEditParserPopup({json}, {idx})");
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "get_parser_names")
                    {
                        var names = ReadParsersFromFile().Select(p => p.ContainsKey("Name") ? p["Name"] : null).Where(n => !string.IsNullOrEmpty(n)).ToList();
                        string json = JsonSerializer.Serialize(names);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.setParserNameOptions({json})");
                    }
                    else if (msg["action"].ToString() == "get_collectors")
                    {
                        await ReloadAndSendCollectors();
                    }
                    else if (msg["action"].ToString() == "get_log_types")
                    {
                        var logTypes = GetLogTypesFromConfig();
                        string json = JsonSerializer.Serialize(logTypes);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"updateLogTypesFromCSharp({JsonSerializer.Serialize(json)})");
                    }
                    // Xử lý các message từ tab Logs
                    else if (msg["action"].ToString() == "get_logs")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElem.GetRawText());
                            if (data != null && data.ContainsKey("logType"))
                            {
                                string logType = data["logType"].ToString();
                                string startTimeStr = data.ContainsKey("startTime") ? data["startTime"].ToString() : "";
                                string endTimeStr = data.ContainsKey("endTime") ? data["endTime"].ToString() : "";
                                
                                var logs = ReadLogsFromFile(logType, startTimeStr, endTimeStr);
                                string json = JsonSerializer.Serialize(logs);
                                await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateLogsFromCSharpForLogsModule({JsonSerializer.Serialize(json)})");
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "get_log_file_path")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElem.GetRawText());
                            if (data != null && data.ContainsKey("logType"))
                            {
                                string logType = data["logType"].ToString();
                                string actualFilePath = GetLogFilePathFromConfig(logType);
                                
                                // Gửi đường dẫn thực tế về JavaScript
                                await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateLogFilePathFromCSharpForLogsModule('{logType}', '{actualFilePath}')");
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "view_log_file")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElem.GetRawText());
                            if (data != null && data.ContainsKey("logType"))
                            {
                                string logType = data["logType"].ToString();
                                string filePath = data.ContainsKey("filePath") ? data["filePath"].ToString() : "";
                                
                                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                                {
                                    // Mở file log bằng notepad hoặc default editor
                                    System.Diagnostics.Process.Start("notepad.exe", filePath);
                                }
                                else
                                {
                                    // Hiển thị thông báo lỗi
                                    await webView2.CoreWebView2.ExecuteScriptAsync($"alert('Không thể mở file log: {filePath}')");
                                }
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "clear_logs")
                    {
                        if (msg.ContainsKey("logType"))
                        {
                            string logType = msg["logType"].ToString();
                            await ClearLogsFromFile(logType);
                            // Gửi lại danh sách log rỗng
                            string json = JsonSerializer.Serialize(new List<string>());
                            await webView2.CoreWebView2.ExecuteScriptAsync($"updateLogsFromCSharp({JsonSerializer.Serialize(json)})");
                        }
                    }
                    else if (msg["action"].ToString() == "start_fluentbit")
                    {
                        StartFluentBit();
                    }
                    else if (msg["action"].ToString() == "stop_fluentbit")
                    {
                        StopFluentBit();
                    }
                    else if (msg["action"].ToString() == "start_fluentbit_console")
                    {
                        StartFluentBitWithConsole();
                    }
                    else if (msg["action"].ToString() == "start_fluentbit_output")
                    {
                        StartFluentBitWithOutputRedirect();
                    }
                    else if (msg["action"].ToString() == "check_fluentbit_status")
                    {
                        Console.WriteLine("🔍 Checking Fluent Bit status...");
                        var isRunning = FluentBitHelper.IsFluentBitRunning();
                        var processes = FluentBitHelper.GetFluentBitProcesses();
                        var statusInfo = new
                        {
                            isRunning = isRunning,
                            processCount = processes.Length,
                            processes = processes.Select(p => new
                            {
                                id = p.Id,
                                startTime = p.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                                cpuTime = p.TotalProcessorTime.TotalSeconds,
                                memoryUsage = p.WorkingSet64 / 1024 / 1024 // MB
                            }).ToArray()
                        };
                        string json = JsonSerializer.Serialize(statusInfo);
                        Console.WriteLine($"📊 Status info: {json}");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateFluentBitStatusFromCSharp({JsonSerializer.Serialize(json)})");
                        Console.WriteLine("✅ Status sent to JavaScript");
                    }
                    else if (msg["action"].ToString() == "debug_log_file")
                    {
                        if (msg.ContainsKey("logType"))
                        {
                            string logType = msg["logType"].ToString();
                            var debugInfo = await DebugLogFile(logType);
                            string json = JsonSerializer.Serialize(debugInfo);
                            await webView2.CoreWebView2.ExecuteScriptAsync($"showDebugInfo({JsonSerializer.Serialize(json)})");
                        }
                    }
                    // Config Tab Actions
                    else if (msg["action"].ToString() == "get_service_config")
                    {
                        var serviceConfig = GetServiceConfig();
                        string json = JsonSerializer.Serialize(serviceConfig);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateServiceConfigFromCSharp({JsonSerializer.Serialize(json)})");
                    }
                    else if (msg["action"].ToString() == "save_service_config")
                    {
                        if (msg.ContainsKey("data") && msg["data"] is System.Text.Json.JsonElement dataElem)
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElem.GetRawText());
                            if (data != null)
                            {
                                SaveServiceConfig(data);
                                await webView2.CoreWebView2.ExecuteScriptAsync($"alert('Đã lưu cấu hình Service thành công!')");
                            }
                        }
                    }
                    else if (msg["action"].ToString() == "get_file_paths")
                    {
                        Console.WriteLine("📁 Getting file paths...");
                        var filePaths = GetFilePaths();
                        string json = JsonSerializer.Serialize(filePaths);
                        Console.WriteLine($"📁 File paths: {json}");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateFilePathsFromCSharpForConfigModule({json})");
                        Console.WriteLine("✅ File paths sent to JavaScript");
                    }
                    else if (msg["action"].ToString() == "backup_config")
                    {
                        var result = BackupConfig();
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('{result}')");
                    }
                    else if (msg["action"].ToString() == "restore_config")
                    {
                        var result = RestoreConfig();
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('{result}')");
                    }
                    else if (msg["action"].ToString() == "view_config")
                    {
                        var configContent = GetConfigPreview();
                        string json = JsonSerializer.Serialize(configContent);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateConfigPreviewFromCSharp({JsonSerializer.Serialize(json)})");
                    }
                    else if (msg["action"].ToString() == "restart_fluentbit")
                    {
                        RestartFluentBit();
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('Đã khởi động lại Fluent Bit!')");
                    }
                    else if (msg["action"].ToString() == "view_fluentbit_logs")
                    {
                        ViewFluentBitLogs();
                    }
                    else if (msg["action"].ToString() == "export_logs")
                    {
                        var result = ExportLogs();
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('{result}')");
                    }
                    else if (msg["action"].ToString() == "restart_application")
                    {
                        RestartApplication();
                    }
                    // Config Sync Management
                    else if (msg["action"].ToString() == "get_config_sync_status")
                    {
                        var syncStatus = GetConfigSyncStatus();
                        var syncJson = JsonSerializer.Serialize(syncStatus);
                        Console.WriteLine($"🔄 Sending config sync status: {syncJson}");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateConfigSyncStatusFromCSharp({syncJson})");
                    }
                    else if (msg["action"].ToString() == "enable_config_sync")
                    {
                        await EnableConfigSync();
                        // Refresh config sync status sau khi bật
                        var syncStatus = GetConfigSyncStatus();
                        var syncJson = JsonSerializer.Serialize(syncStatus);
                        Console.WriteLine($"🔄 [ENABLE] Sending updated sync status: {syncJson}");
                        Console.WriteLine($"🔄 [ENABLE] syncStatus.enabled = {syncStatus["enabled"]}");
                        Console.WriteLine($"🔄 [ENABLE] syncStatus.apiUrl = {syncStatus["apiUrl"]}");
                        Console.WriteLine($"🔄 [ENABLE] syncStatus.lastSyncTime = {syncStatus["lastSyncTime"]}");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateConfigSyncStatusFromCSharp({syncJson})");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('✅ Đã bật đồng bộ config từ API')");
                    }
                    else if (msg["action"].ToString() == "disable_config_sync")
                    {
                        await DisableConfigSync();
                        // Refresh config sync status sau khi tắt
                        var syncStatus = GetConfigSyncStatus();
                        var syncJson = JsonSerializer.Serialize(syncStatus);
                        Console.WriteLine($"🔄 [DISABLE] Sending updated sync status: {syncJson}");
                        Console.WriteLine($"🔄 [DISABLE] syncStatus.enabled = {syncStatus["enabled"]}");
                        Console.WriteLine($"🔄 [DISABLE] syncStatus.apiUrl = {syncStatus["apiUrl"]}");
                        Console.WriteLine($"🔄 [DISABLE] syncStatus.lastSyncTime = {syncStatus["lastSyncTime"]}");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateConfigSyncStatusFromCSharp({syncJson})");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('❌ Đã tắt đồng bộ config')");
                    }
                    else if (msg["action"].ToString() == "manual_config_sync")
                    {
                        await ManualConfigSync();
                        // Refresh config sync status sau khi đồng bộ thủ công
                        var syncStatus = GetConfigSyncStatus();
                        var syncJson = JsonSerializer.Serialize(syncStatus);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"window.updateConfigSyncStatusFromCSharp({syncJson})");
                        await webView2.CoreWebView2.ExecuteScriptAsync($"alert('🔄 Đã thực hiện đồng bộ config thủ công')");
                    }
                    else if (msg["action"].ToString() == "view_sync_logs")
                    {
                        await ViewSyncLogs();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xử lý message: " + ex.Message);
            }
        }

        private void UpdateOutputBlockByTag(string tag, bool enable)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
            var lines = File.ReadAllLines(configPath).ToList();
            
            // Xóa tất cả block OUTPUT cho tag này
            int i = 0;
            while (i < lines.Count)
            {
                if (lines[i].Trim().StartsWith("[OUTPUT", StringComparison.OrdinalIgnoreCase))
                {
                    int start = i;
                    int end = i + 1;
                    string matchTag = null;
                    while (end < lines.Count && !lines[end].Trim().StartsWith("["))
                    {
                        var line = lines[end].Trim();
                        if (line.StartsWith("Match", StringComparison.OrdinalIgnoreCase))
                        {
                            matchTag = line.Substring(5).Trim();
                        }
                        end++;
                    }
                    if (matchTag == tag)
                    {
                        lines.RemoveRange(start, end - start);
                        continue; // Không tăng i vì đã remove
                    }
                }
                i++;
            }
            
            // Nếu enable thì thêm lại block OUTPUT cho tag này (chỉ khi chưa tồn tại)
            if (enable)
            {
                // Kiểm tra lại tránh trùng lặp
                bool exists = false;
                for (int k = 0; k < lines.Count; k++)
                {
                    if (lines[k].Trim().StartsWith("[OUTPUT", StringComparison.OrdinalIgnoreCase))
                    {
                        int l = k + 1;
                        string matchTag = null;
                        while (l < lines.Count && !lines[l].Trim().StartsWith("["))
                        {
                            var line = lines[l].Trim();
                            if (line.StartsWith("Match", StringComparison.OrdinalIgnoreCase))
                                matchTag = line.Substring(5).Trim();
                            l++;
                        }
                        if (matchTag == tag)
                        {
                            exists = true;
                            break;
                        }
                    }
                }
                
                if (!exists)
                {
                    // Mặc định tạo output kiểu file
                    var block = new List<string>
                    {
                        "[OUTPUT]",
                        "    Name    file",
                        $"    Match    {tag}",
                        "    Path    .\\logs\\",
                        $"    File    {tag}.log",
                        "    Format    plain",
                        "    Retry_Limit    3",
                        ""
                    };
                    lines.AddRange(block);
                }
            }
            
            File.WriteAllLines(configPath, lines);
            // Có thể gọi FluentBitHelper.RestartFluentBitWithNotify(); nếu muốn restart dịch vụ
        }

        // Hàm mới: Thêm collector với output tùy chỉnh
        private void AddCollectorWithOutput(Dictionary<string, object> data)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
            var lines = File.ReadAllLines(configPath).ToList();
            
            // Thêm INPUT block
            var inputBlock = new List<string> { "[INPUT]" };
            if (data.ContainsKey("type"))
            {
                inputBlock.Add($"    Name    {data["type"]}");
            }
            
            foreach (var kv in data)
            {
                if (kv.Key == "type" || kv.Key == "outputs") continue;
                inputBlock.Add($"    {kv.Key}    {kv.Value}");
            }
            inputBlock.Add("");
            lines.AddRange(inputBlock);
            
            // Thêm OUTPUT blocks nếu có
            if (data.ContainsKey("outputs") && data["outputs"] is List<Dictionary<string, object>> outputs)
            {
                foreach (var output in outputs)
                {
                    if (output.ContainsKey("type"))
                    {
                        var outputBlock = new List<string> { "[OUTPUT]" };
                        outputBlock.Add($"    Name    {output["type"]}");
                        outputBlock.Add($"    Match    {data["Tag"]}");
                        
                        // Thêm các tham số khác của output
                        foreach (var outputParam in output)
                        {
                            if (outputParam.Key == "type") continue;
                            outputBlock.Add($"    {outputParam.Key}    {outputParam.Value}");
                        }
                        outputBlock.Add("");
                        lines.AddRange(outputBlock);
                    }
                }
            }
            else
            {
                // Mặc định tạo output kiểu file
                var defaultOutput = new List<string>
                {
                    "[OUTPUT]",
                    "    Name    file",
                    $"    Match    {data["Tag"]}",
                    "    Path    .\\logs\\",
                    $"    File    {data["Tag"]}.log",
                    "    Format    plain",
                    "    Retry_Limit    3",
                    ""
                };
                lines.AddRange(defaultOutput);
            }
            
            File.WriteAllLines(configPath, lines);
        }

        private void AddInputBlockDynamic(Dictionary<string, string> data)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
            var lines = File.ReadAllLines(configPath).ToList();
            var block = new List<string> { "[INPUT]" };
            if (data.ContainsKey("type"))
            {
                block.Add($"    Name    {data["type"]}");
            }
            foreach (var kv in data)
            {
                if (kv.Key == "type") continue;
                block.Add($"    {kv.Key}    {kv.Value}");
            }
            block.Add("");
            lines.AddRange(block);
            File.WriteAllLines(configPath, lines);
        }

        private async System.Threading.Tasks.Task ReloadAndSendCollectors()
        {
            Console.WriteLine("🔄 ReloadAndSendCollectors: Bắt đầu đọc file fluent-bit.conf");
            
            var collectors = new List<object>();
            
            // Sử dụng lại logic từ FluentBitHelper để đảm bảo tính nhất quán
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
            Console.WriteLine($"📁 Đường dẫn config: {configPath}");
            
            if (!File.Exists(configPath)) {
                Console.WriteLine($"❌ Không tìm thấy file config tại: {configPath}");
                return;
            }
            
            var lines = File.ReadAllLines(configPath);
            Console.WriteLine($"📄 Đọc được {lines.Length} dòng từ fluent-bit.conf");
            Console.WriteLine("📄 Nội dung file fluent-bit.conf:");
            for (int i = 0; i < lines.Length; i++)
            {
                Console.WriteLine($"  [{i + 1:000}] {lines[i]}");
            }
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🔍 Tìm thấy INPUT section tại dòng {i + 1}");
                    string name = "", tag = "", type = "";
                    int j = i + 1;
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        var line = lines[j].Trim();
                        if (line.StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                            type = line.Substring(4).Trim();
                        if (line.StartsWith("Tag", StringComparison.OrdinalIgnoreCase))
                            tag = line.Substring(3).Trim();
                        if (line.StartsWith("Alias", StringComparison.OrdinalIgnoreCase))
                            name = line.Substring(5).Trim();
                        j++;
                    }
                    if (string.IsNullOrEmpty(name)) name = type;
                    if (string.IsNullOrEmpty(tag)) tag = name;
                    
                    Console.WriteLine($"📊 Collector: name={name}, type={type}, tag={tag}");
                    
                    bool isEnabled = false;
                    for (int k = 0; k < lines.Length; k++)
                    {
                        if (lines[k].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                        {
                            int l = k + 1;
                            string matchTag = "";
                            while (l < lines.Length && !lines[l].Trim().StartsWith("["))
                            {
                                var outLine = lines[l].Trim();
                                if (outLine.StartsWith("Match", StringComparison.OrdinalIgnoreCase))
                                {
                                    matchTag = outLine.Substring(5).Trim();
                                    break;
                                }
                                l++;
                            }
                            if (matchTag == tag)
                            {
                                isEnabled = true;
                                Console.WriteLine($"✅ Collector {tag} được enable bởi OUTPUT Match={matchTag}");
                                break;
                            }
                        }
                    }
                    
                    var collector = new
                    {
                        name,
                        type,
                        tag,
                        status = isEnabled ? "running" : "stopped"
                    };
                    collectors.Add(collector);
                    Console.WriteLine($"➕ Thêm collector: {JsonSerializer.Serialize(collector)}");
                }
            }
            
            string json = JsonSerializer.Serialize(collectors);
            Console.WriteLine($"📤 Gửi {collectors.Count} collectors đến JavaScript: {json}");
            
            try
            {
                await webView2.CoreWebView2.ExecuteScriptAsync($"updateCollectorsFromCSharp({JsonSerializer.Serialize(json)})");
                Console.WriteLine("✅ Đã gửi dữ liệu collectors thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi gửi dữ liệu collectors: {ex.Message}");
            }
        }

        private List<Dictionary<string, string>> ReadParsersFromFile()
        {
            var result = new List<Dictionary<string, string>>();
            string parsersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parsers.conf");
            if (!File.Exists(parsersPath)) return result;
            var lines = File.ReadAllLines(parsersPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[PARSER]", StringComparison.OrdinalIgnoreCase))
                {
                    var parser = new Dictionary<string, string>();
                    int j = i + 1;
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        var line = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(line)) { j++; continue; }
                        var idx = line.IndexOf(' ');
                        if (idx > 0)
                        {
                            var key = line.Substring(0, idx).Trim();
                            var value = line.Substring(idx).Trim();
                            parser[key] = value;
                        }
                        j++;
                    }
                    result.Add(parser);
                }
            }
            return result;
        }

        private void AddParserBlock(Dictionary<string, string> data)
        {
            var block = new List<string> { "[PARSER]" };
            foreach (var kv in data)
            {
                block.Add($"    {kv.Key}    {kv.Value}");
            }
            block.Add("");
            string parsersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parsers.conf");
            File.AppendAllLines(parsersPath, block);
        }

        private void EditParserBlock(int idx, Dictionary<string, string> data)
        {
            string parsersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parsers.conf");
            var lines = File.ReadAllLines(parsersPath).ToList();
            int count = -1;
            int start = -1, end = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals("[PARSER]", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    if (count == idx)
                    {
                        start = i;
                        end = i + 1;
                        while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                        break;
                    }
                }
            }
            if (start != -1 && end != -1)
            {
                lines.RemoveRange(start, end - start);
                var block = new List<string> { "[PARSER]" };
                foreach (var kv in data)
                {
                    block.Add($"    {kv.Key}    {kv.Value}");
                }
                block.Add("");
                lines.InsertRange(start, block);
                File.WriteAllLines(parsersPath, lines);
            }
        }

        private void DeleteParserBlock(int idx)
        {
            string parsersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parsers.conf");
            var lines = File.ReadAllLines(parsersPath).ToList();
            int count = -1;
            int start = -1, end = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals("[PARSER]", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    if (count == idx)
                    {
                        start = i;
                        end = i + 1;
                        while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                        break;
                    }
                }
            }
            if (start != -1 && end != -1)
            {
                lines.RemoveRange(start, end - start);
                File.WriteAllLines(parsersPath, lines);
            }
        }

        // Lấy danh sách log types từ fluent-bit.conf
        private List<object> GetLogTypesFromConfig()
        {
            var logTypes = new List<object>();
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
                if (!File.Exists(configPath)) return logTypes;

                var lines = File.ReadAllLines(configPath);
                var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"winlog", "Windows Event Log"},
                    {"syslog", "Syslog"},
                    {"odbc", "ODBC Database"},
                    {"jdbc", "JDBC Database"},
                    {"ftp", "FTP"},
                    {"http", "HTTP"},
                    {"tail", "File Tail"},
                    {"dummy", "Dummy"},
                    {"win_stat", "Windows Statistics"}
                };

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                    {
                        string name = "", tag = "", type = "";
                        int j = i + 1;
                        while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                        {
                            var line = lines[j].Trim();
                            if (line.StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                                type = line.Substring(4).Trim();
                            if (line.StartsWith("Tag", StringComparison.OrdinalIgnoreCase))
                                tag = line.Substring(3).Trim();
                            if (line.StartsWith("Alias", StringComparison.OrdinalIgnoreCase))
                                name = line.Substring(5).Trim();
                            j++;
                        }
                        
                        if (!string.IsNullOrEmpty(type))
                        {
                            string displayName = friendlyNames.ContainsKey(type) ? friendlyNames[type] : type;
                            if (!string.IsNullOrEmpty(tag))
                                displayName += $" [Tag={tag}]";
                            
                            logTypes.Add(new
                            {
                                name = type,  // Name của INPUT (winlog, syslog, etc.)
                                tag = tag,    // Tag của INPUT
                                displayName = displayName
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc log types: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return logTypes;
        }

        // Methods xử lý Logs
        private List<string> ReadLogsFromFile(string logType, string startTimeStr, string endTimeStr)
        {
            var logs = new List<string>();
            try
            {
                // Lấy đường dẫn file log từ cấu hình
                string logFilePath = GetLogFilePathFromConfig(logType);
                
                if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
                {
                    Console.WriteLine($"File log không tồn tại: {logFilePath}");
                    return logs;
                }
                
                // Đọc tất cả dòng từ file log
                var lines = File.ReadAllLines(logFilePath);
                
                // Parse thời gian nếu có
                DateTime? start = null;
                DateTime? end = null;
                
                if (!string.IsNullOrEmpty(startTimeStr))
                {
                    if (DateTime.TryParse(startTimeStr, out DateTime startDt))
                        start = startDt;
                }
                
                if (!string.IsNullOrEmpty(endTimeStr))
                {
                    if (DateTime.TryParse(endTimeStr, out DateTime endDt))
                        end = endDt;
                }
                
                // Lọc logs theo thời gian nếu có
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    try
                    {
                        // Parse JSON để lấy thời gian
                        var logObj = JsonSerializer.Deserialize<JsonElement>(line);
                        if (logObj.TryGetProperty("TimeGenerated", out JsonElement timeElement))
                        {
                            string timeStr = timeElement.GetString();
                            if (DateTime.TryParse(timeStr, out DateTime logTime))
                            {
                                // Kiểm tra thời gian
                                if (start.HasValue && logTime < start.Value) continue;
                                if (end.HasValue && logTime > end.Value) continue;
                            }
                        }
                        
                        logs.Add(line);
                    }
                    catch
                    {
                        // Nếu không parse được JSON, vẫn thêm vào
                        logs.Add(line);
                    }
                }
                
                // Debug: Log số lượng log tìm được
                Console.WriteLine($"Tìm thấy {logs.Count} log entries cho {logType} từ {startTimeStr} đến {endTimeStr}");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đọc file log: {ex.Message}");
                logs.Add($"Lỗi đọc file log: {ex.Message}");
            }
            
            return logs;
        }

        private async System.Threading.Tasks.Task ClearLogsFromFile(string logType)
        {
            try
            {
                if (_logManagementService == null)
                {
                    MessageBox.Show("LogManagementService chưa được khởi tạo!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                await _logManagementService.ClearLogsAsync(logType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa log: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartFluentBit()
        {
            try
            {
                FluentBitHelper.StartFluentBit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động Fluent Bit: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopFluentBit()
        {
            try
            {
                FluentBitHelper.StopFluentBit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi dừng Fluent Bit: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartFluentBitWithConsole()
        {
            try
            {
                FluentBitHelper.StartFluentBitWithConsole();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động Fluent Bit với Console: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartFluentBitWithOutputRedirect()
        {
            try
            {
                FluentBitHelper.StartFluentBitWithOutputRedirect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động Fluent Bit với Output Redirect: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task<object> DebugLogFile(string logType)
        {
            try
            {
                string logFilePath = Path.Combine("logs", $"{logType}.log");
                if (!File.Exists(logFilePath))
                {
                    return new { message = $"Không tìm thấy file log cho {logType} tại: {logFilePath}" };
                }

                var lines = await File.ReadAllLinesAsync(logFilePath);
                var debugInfo = new List<string>();
                debugInfo.Add($"File: {logFilePath}");
                debugInfo.Add($"Số dòng: {lines.Length}");
                debugInfo.Add($"Kích thước: {new FileInfo(logFilePath).Length} bytes");
                debugInfo.Add($"Ngày tạo: {File.GetCreationTime(logFilePath).ToString("yyyy-MM-dd HH:mm:ss")}");
                debugInfo.Add($"Ngày sửa đổi: {File.GetLastWriteTime(logFilePath).ToString("yyyy-MM-dd HH:mm:ss")}");
                debugInfo.Add($"Ngày truy cập: {File.GetLastAccessTime(logFilePath).ToString("yyyy-MM-dd HH:mm:ss")}");

                debugInfo.Add("Nội dung đầu tiên (nếu có):");
                if (lines.Length > 0)
                {
                    debugInfo.Add(lines[0]);
                }
                else
                {
                    debugInfo.Add("Không có dữ liệu.");
                }

                debugInfo.Add("Nội dung cuối cùng (nếu có):");
                if (lines.Length > 0)
                {
                    debugInfo.Add(lines[lines.Length - 1]);
                }
                else
                {
                    debugInfo.Add("Không có dữ liệu.");
                }

                return new { debugInfo = debugInfo };
            }
            catch (Exception ex)
            {
                return new { message = $"Lỗi debug log file: {ex.Message}" };
            }
        }

        private string GetLogFilePathFromConfig(string logType)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
                if (!File.Exists(configPath)) return "File cấu hình không tồn tại";
                
                var lines = File.ReadAllLines(configPath);
                string currentInputTag = null;
                string currentOutputPath = null;
                string currentOutputFile = null;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    
                    // Tìm [INPUT] block
                    if (line == "[INPUT]")
                    {
                        // Tìm Tag trong INPUT block
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            string inputLine = lines[j].Trim();
                            if (inputLine.StartsWith("[") && inputLine.EndsWith("]")) break; // Block mới
                            if (inputLine.StartsWith("Tag"))
                            {
                                string tagValue = inputLine.Substring(3).Trim();
                                if (tagValue.ToLower() == logType.ToLower())
                                {
                                    currentInputTag = tagValue;
                                    break;
                                }
                            }
                        }
                    }
                    // Tìm [OUTPUT] block
                    else if (line == "[OUTPUT]")
                    {
                        currentOutputPath = null;
                        currentOutputFile = null;
                        // Tìm Match và Path, File trong OUTPUT block
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            string outputLine = lines[j].Trim();
                            if (outputLine.StartsWith("[") && outputLine.EndsWith("]")) break; // Block mới
                            
                            if (outputLine.StartsWith("Match") && currentInputTag != null)
                            {
                                string matchValue = outputLine.Substring(5).Trim();
                                if (matchValue == currentInputTag)
                                {
                                    // Tìm Path và File
                                    for (int k = j + 1; k < lines.Length; k++)
                                    {
                                        string paramLine = lines[k].Trim();
                                        if (paramLine.StartsWith("[") && paramLine.EndsWith("]")) break;
                                        
                                        if (paramLine.StartsWith("Path"))
                                            currentOutputPath = paramLine.Substring(4).Trim();
                                        else if (paramLine.StartsWith("File"))
                                            currentOutputFile = paramLine.Substring(4).Trim();
                                    }
                                    break;
                                }
                            }
                        }
                        
                        // Nếu tìm thấy đường dẫn cho logType này
                        if (currentInputTag != null && currentInputTag.ToLower() == logType.ToLower() && 
                            !string.IsNullOrEmpty(currentOutputPath) && !string.IsNullOrEmpty(currentOutputFile))
                        {
                            // Sử dụng Path.Combine để tạo đường dẫn an toàn
                            return Path.Combine(currentOutputPath, currentOutputFile);
                        }
                    }
                }
                
                return $"Không tìm thấy cấu hình cho {logType}";
            }
            catch (Exception ex)
            {
                return $"Lỗi đọc cấu hình: {ex.Message}";
            }
        }

        // Config Tab Methods
        private Dictionary<string, object> GetServiceConfig()
        {
            var config = new Dictionary<string, object>();
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
                if (!File.Exists(configPath)) return config;
                
                var lines = File.ReadAllLines(configPath);
                bool inServiceBlock = false;
                
                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    if (trimmedLine == "[SERVICE]")
                    {
                        inServiceBlock = true;
                        continue;
                    }
                    else if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        inServiceBlock = false;
                        continue;
                    }
                    
                    if (inServiceBlock && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        var parts = trimmedLine.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            
                            switch (key.ToLower())
                            {
                                case "flush":
                                    if (int.TryParse(value, out int flush))
                                        config["flushInterval"] = flush;
                                    break;
                                case "log_level":
                                    config["logLevel"] = value.ToLower();
                                    break;
                                case "daemon":
                                    config["daemonMode"] = value;
                                    break;
                                case "parsers_file":
                                    config["parsersFile"] = value;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đọc service config: {ex.Message}");
            }
            
            return config;
        }

        private void SaveServiceConfig(Dictionary<string, object> data)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
                if (!File.Exists(configPath)) return;
                
                var lines = File.ReadAllLines(configPath).ToList();
                int serviceStart = -1;
                int serviceEnd = -1;
                
                // Tìm [SERVICE] block
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim() == "[SERVICE]")
                    {
                        serviceStart = i;
                        serviceEnd = i + 1;
                        while (serviceEnd < lines.Count && !lines[serviceEnd].Trim().StartsWith("["))
                        {
                            serviceEnd++;
                        }
                        break;
                    }
                }
                
                // Nếu không có [SERVICE] block, tạo mới
                if (serviceStart == -1)
                {
                    serviceStart = 0;
                    serviceEnd = 0;
                    lines.Insert(0, "[SERVICE]");
                    lines.Insert(1, "");
                }
                
                // Xóa nội dung cũ của [SERVICE] block
                lines.RemoveRange(serviceStart + 1, serviceEnd - serviceStart - 1);
                
                // Thêm cấu hình mới
                int insertIndex = serviceStart + 1;
                
                if (data.ContainsKey("flushInterval"))
                {
                    lines.Insert(insertIndex++, $"    Flush    {data["flushInterval"]}");
                }
                
                if (data.ContainsKey("logLevel"))
                {
                    lines.Insert(insertIndex++, $"    Log_Level    {data["logLevel"]}");
                }
                
                if (data.ContainsKey("daemonMode"))
                {
                    lines.Insert(insertIndex++, $"    Daemon    {data["daemonMode"]}");
                }
                
                if (data.ContainsKey("parsersFile"))
                {
                    lines.Insert(insertIndex++, $"    Parsers_File    {data["parsersFile"]}");
                }
                
                // Thêm dòng trống
                lines.Insert(insertIndex, "");
                
                // Lưu file
                File.WriteAllLines(configPath, lines);
                Console.WriteLine("Đã lưu service config thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lưu service config: {ex.Message}");
                throw;
            }
        }

        private Dictionary<string, string> GetFilePaths()
        {
            var paths = new Dictionary<string, string>();
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configFile = Path.Combine(baseDir, "fluent-bit.conf");
                
                Console.WriteLine($"📁 Base directory: {baseDir}");
                Console.WriteLine($"📁 Reading from config file: {configFile}");
                
                // Đọc từ fluent-bit.conf để lấy đường dẫn thực tế
                if (File.Exists(configFile))
                {
                    var lines = File.ReadAllLines(configFile);
                    string currentParsersFile = "parsers.conf"; // Default
                    string currentLogDir = Path.Combine(baseDir, "logs"); // Default
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        // Tìm Parsers_File trong [SERVICE] block
                        if (trimmedLine.StartsWith("Parsers_File", StringComparison.OrdinalIgnoreCase))
                        {
                            currentParsersFile = trimmedLine.Substring(12).Trim();
                            Console.WriteLine($"📁 Found Parsers_File: {currentParsersFile}");
                        }
                        
                        // Tìm Path trong [OUTPUT] blocks
                        if (trimmedLine.StartsWith("Path", StringComparison.OrdinalIgnoreCase))
                        {
                            string pathValue = trimmedLine.Substring(4).Trim();
                            if (!string.IsNullOrEmpty(pathValue))
                            {
                                // Xử lý relative path (ví dụ: .\logs\)
                                if (pathValue.StartsWith(".\\") || pathValue.StartsWith("./"))
                                {
                                    // Loại bỏ .\ hoặc ./
                                    string relativePath = pathValue.Substring(2);
                                    currentLogDir = Path.Combine(baseDir, relativePath);
                                }
                                else if (!Path.IsPathRooted(pathValue))
                                {
                                    // Path tương đối khác
                                    currentLogDir = Path.Combine(baseDir, pathValue);
                                }
                                else
                                {
                                    // Path tuyệt đối
                                    currentLogDir = pathValue;
                                }
                                Console.WriteLine($"📁 Found Path in OUTPUT: {pathValue} -> {currentLogDir}");
                                break; // Lấy path đầu tiên tìm thấy
                            }
                        }
                    }
                    
                    paths["configPath"] = configFile;
                    paths["parsersPath"] = Path.Combine(baseDir, currentParsersFile);
                    paths["logDir"] = currentLogDir;
                }
                else
                {
                    // Fallback nếu file không tồn tại
                    paths["configPath"] = configFile;
                    paths["parsersPath"] = Path.Combine(baseDir, "parsers.conf");
                    paths["logDir"] = Path.Combine(baseDir, "logs");
                    Console.WriteLine("⚠️ Config file not found, using default paths");
                }
                
                Console.WriteLine($"📁 Final paths:");
                Console.WriteLine($"📁 Config path: {paths["configPath"]}");
                Console.WriteLine($"📁 Parsers path: {paths["parsersPath"]}");
                Console.WriteLine($"📁 Log directory: {paths["logDir"]}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy file paths: {ex.Message}");
                // Fallback trong trường hợp lỗi
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                paths["configPath"] = Path.Combine(baseDir, "fluent-bit.conf");
                paths["parsersPath"] = Path.Combine(baseDir, "parsers.conf");
                paths["logDir"] = Path.Combine(baseDir, "logs");
            }
            return paths;
        }

        private string BackupConfig()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = Path.Combine(baseDir, "backup");
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);
                
                string configFile = Path.Combine(baseDir, "fluent-bit.conf");
                string parsersFile = Path.Combine(baseDir, "parsers.conf");
                
                if (File.Exists(configFile))
                {
                    string backupConfig = Path.Combine(backupDir, $"fluent-bit_{timestamp}.conf");
                    File.Copy(configFile, backupConfig);
                }
                
                if (File.Exists(parsersFile))
                {
                    string backupParsers = Path.Combine(backupDir, $"parsers_{timestamp}.conf");
                    File.Copy(parsersFile, backupParsers);
                }
                
                return $"Đã backup cấu hình thành công vào thư mục: {backupDir}";
            }
            catch (Exception ex)
            {
                return $"Lỗi backup: {ex.Message}";
            }
        }

        private string RestoreConfig()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = Path.Combine(baseDir, "backup");
                
                if (!Directory.Exists(backupDir))
                    return "Không tìm thấy thư mục backup";
                
                var backupFiles = Directory.GetFiles(backupDir, "*.conf")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();
                
                if (backupFiles.Count == 0)
                    return "Không tìm thấy file backup";
                
                // Lấy file backup mới nhất
                string latestBackup = backupFiles.First();
                string fileName = Path.GetFileName(latestBackup);
                
                if (fileName.StartsWith("fluent-bit_"))
                {
                    string configFile = Path.Combine(baseDir, "fluent-bit.conf");
                    File.Copy(latestBackup, configFile, true);
                    return $"Đã restore fluent-bit.conf từ: {fileName}";
                }
                else if (fileName.StartsWith("parsers_"))
                {
                    string parsersFile = Path.Combine(baseDir, "parsers.conf");
                    File.Copy(latestBackup, parsersFile, true);
                    return $"Đã restore parsers.conf từ: {fileName}";
                }
                
                return "File backup không hợp lệ";
            }
            catch (Exception ex)
            {
                return $"Lỗi restore: {ex.Message}";
            }
        }

        private string GetConfigPreview()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluent-bit.conf");
                if (File.Exists(configPath))
                {
                    return File.ReadAllText(configPath);
                }
                return "File cấu hình không tồn tại";
            }
            catch (Exception ex)
            {
                return $"Lỗi đọc config: {ex.Message}";
            }
        }

        private void RestartFluentBit()
        {
            try
            {
                StopFluentBit();
                System.Threading.Thread.Sleep(2000); // Đợi 2 giây
                StartFluentBit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi restart Fluent Bit: {ex.Message}");
            }
        }

        private void ViewFluentBitLogs()
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logDir);
                }
                else
                {
                    MessageBox.Show("Thư mục logs không tồn tại", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở thư mục logs: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ExportLogs()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDir = Path.Combine(baseDir, "logs");
                string exportDir = Path.Combine(baseDir, "exports");
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                if (!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);
                
                if (!Directory.Exists(logDir))
                    return "Thư mục logs không tồn tại";
                
                string exportPath = Path.Combine(exportDir, $"logs_{timestamp}.zip");
                
                // Tạo file zip chứa tất cả logs
                System.IO.Compression.ZipFile.CreateFromDirectory(logDir, exportPath);
                
                return $"Đã export logs thành công: {exportPath}";
            }
            catch (Exception ex)
            {
                return $"Lỗi export logs: {ex.Message}";
            }
        }

        private void RestartApplication()
        {
            try
            {
                // Lưu trạng thái hiện tại nếu cần
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                System.Diagnostics.Process.Start(exePath);
                
                // Đóng ứng dụng hiện tại
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động lại ứng dụng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Config Sync Management Methods
        private Dictionary<string, object> GetConfigSyncStatus()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
                Console.WriteLine($"📁 [GetConfigSyncStatus] Config path: {configPath}");
                Console.WriteLine($"📁 [GetConfigSyncStatus] File exists: {File.Exists(configPath)}");
                
                if (!File.Exists(configPath))
                {
                    Console.WriteLine("❌ [GetConfigSyncStatus] File agent_config.json không tồn tại");
                    return new Dictionary<string, object>
                    {
                        ["enabled"] = false,
                        ["lastSyncTime"] = "Chưa cấu hình",
                        ["apiUrl"] = "Chưa cấu hình"
                    };
                }

                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                
                bool enabled = false;
                string apiUrl = "Chưa cấu hình";
                string lastSyncTime = "Chưa đồng bộ";

                if (config.ContainsKey("config_sync") && config["config_sync"] is System.Text.Json.JsonElement syncElement)
                {
                    var syncConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(syncElement.GetRawText());
                    enabled = syncConfig.ContainsKey("enabled") && (bool)syncConfig["enabled"];
                    apiUrl = syncConfig.ContainsKey("url") ? syncConfig["url"].ToString() : "Chưa cấu hình";
                    
                    Console.WriteLine($"📁 [GetConfigSyncStatus] Raw sync config: {syncElement.GetRawText()}");
                    Console.WriteLine($"📁 [GetConfigSyncStatus] enabled = {enabled}");
                    Console.WriteLine($"📁 [GetConfigSyncStatus] apiUrl = {apiUrl}");
                }

                // Đọc thời gian đồng bộ cuối cùng từ file log
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "agent_config_info.json");
                if (File.Exists(logPath))
                {
                    try
                    {
                        var logJson = File.ReadAllText(logPath);
                        var logData = JsonSerializer.Deserialize<Dictionary<string, object>>(logJson);
                        if (logData.ContainsKey("LastSyncTime"))
                        {
                            lastSyncTime = logData["LastSyncTime"].ToString();
                        }
                    }
                    catch
                    {
                        // Ignore log reading errors
                    }
                }

                return new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                    ["lastSyncTime"] = lastSyncTime,
                    ["apiUrl"] = apiUrl
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi lấy config sync status: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return new Dictionary<string, object>
                {
                    ["enabled"] = false,
                    ["lastSyncTime"] = "Lỗi",
                    ["apiUrl"] = "Lỗi"
                };
            }
        }

        private async Task EnableConfigSync()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
                if (!File.Exists(configPath))
                {
                    throw new Exception("File agent_config.json không tồn tại");
                }

                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                
                if (config.ContainsKey("config_sync") && config["config_sync"] is System.Text.Json.JsonElement syncElement)
                {
                    var syncConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(syncElement.GetRawText());
                    syncConfig["enabled"] = true;
                    
                    // Cập nhật lại config
                    config["config_sync"] = syncConfig;
                    
                    // Lưu file
                    var updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(configPath, updatedJson);
                    
                    Console.WriteLine("✅ Đã bật config sync");
                }
                else
                {
                    throw new Exception("Không tìm thấy cấu hình config_sync");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi bật config sync: {ex.Message}");
                throw;
            }
        }

        private async Task DisableConfigSync()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
                if (!File.Exists(configPath))
                {
                    throw new Exception("File agent_config.json không tồn tại");
                }

                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                
                if (config.ContainsKey("config_sync") && config["config_sync"] is System.Text.Json.JsonElement syncElement)
                {
                    var syncConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(syncElement.GetRawText());
                    syncConfig["enabled"] = false;
                    
                    // Cập nhật lại config
                    config["config_sync"] = syncConfig;
                    
                    // Lưu file
                    var updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(configPath, updatedJson);
                    
                    Console.WriteLine("❌ Đã tắt config sync");
                }
                else
                {
                    throw new Exception("Không tìm thấy cấu hình config_sync");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tắt config sync: {ex.Message}");
                throw;
            }
        }

        private async Task ManualConfigSync()
        {
            try
            {
                // Tạo instance ConfigSyncService và thực hiện đồng bộ thủ công
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
                if (!File.Exists(configPath))
                {
                    throw new Exception("File agent_config.json không tồn tại");
                }

                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                
                if (config.ContainsKey("config_sync") && config["config_sync"] is System.Text.Json.JsonElement syncElement)
                {
                    var syncConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(syncElement.GetRawText());
                    bool enabled = syncConfig.ContainsKey("enabled") && (bool)syncConfig["enabled"];
                    string url = syncConfig.ContainsKey("url") ? syncConfig["url"].ToString() : "";
                    
                    if (!enabled)
                    {
                        throw new Exception("Config sync chưa được bật");
                    }
                    
                    if (string.IsNullOrEmpty(url))
                    {
                        throw new Exception("URL API chưa được cấu hình");
                    }

                    // Tạo ConfigSyncService và thực hiện đồng bộ
                    var syncService = new Core.Services.ConfigSyncService(url, "fluent-bit.conf");
                    syncService.OnConfigUpdated += (sender, configPath) =>
                    {
                        Console.WriteLine($"🔄 Config đã được cập nhật: {configPath}");
                        // Restart Fluent Bit nếu cần
                        RestartFluentBit();
                    };
                    
                    // Thực hiện đồng bộ thủ công
                    await syncService.SyncConfigAsync();
                    
                    // Cập nhật thời gian đồng bộ cuối cùng
                    await UpdateLastSyncTime();
                    
                    Console.WriteLine("✅ Đã thực hiện đồng bộ config thủ công");
                }
                else
                {
                    throw new Exception("Không tìm thấy cấu hình config_sync");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đồng bộ config thủ công: {ex.Message}");
                throw;
            }
        }

        private async Task ViewSyncLogs()
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "config_changes.log");
                if (File.Exists(logPath))
                {
                    var logContent = await File.ReadAllTextAsync(logPath);
                    // Hiển thị log trong popup hoặc window mới
                    MessageBox.Show(logContent, "Config Sync Logs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Chưa có log đồng bộ config", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc sync logs: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateLastSyncTime()
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "agent_config_info.json");
                var configInfo = new
                {
                    LastSyncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    SyncType = "Manual",
                    Status = "Success"
                };
                
                var jsonString = JsonSerializer.Serialize(configInfo, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(logPath, jsonString);
                
                Console.WriteLine($"✅ Đã cập nhật thời gian đồng bộ: {configInfo.LastSyncTime}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi cập nhật thời gian đồng bộ: {ex.Message}");
            }
        }
    }
} 