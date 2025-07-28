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

namespace SIEM_Agent.UI.Forms
{
    public class WebViewForm : Form
    {
        private WebView2 webView2;
        private readonly LogManagementService _logManagementService;

        public WebViewForm()
        {
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
            LoadDashboardHtmlFile();

            // Lắng nghe message từ web (toggle collector)
            webView2.CoreWebView2InitializationCompleted += (s, e) =>
            {
                webView2.CoreWebView2.WebMessageReceived += WebView2_WebMessageReceived;
            };
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
            }
            catch { }
        }

        private void LoadDashboardHtmlFile()
        {
            // Đường dẫn tới file dashboard.html trong thư mục build/output (web folder riêng)
            string htmlPath = Path.Combine(Application.StartupPath, "UI/Forms/WebForm/web/dashboard.html");
            if (!File.Exists(htmlPath))
            {
                MessageBox.Show($"Không tìm thấy file dashboard.html ở: {htmlPath}\n\nHãy kiểm tra lại thuộc tính Copy to Output Directory của file HTML/CSS/JS!", "Lỗi file không tồn tại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            webView2.Source = new Uri(htmlPath);
            webView2.NavigationCompleted += WebView2_NavigationCompleted;
        }

        private async void WebView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            // Sử dụng lại logic của ReloadAndSendCollectors để đảm bảo đồng nhất collector
            await ReloadAndSendCollectors();
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
                    else if (msg["action"].ToString() == "get_log_types")
                    {
                        var logTypes = GetLogTypesFromConfig();
                        string json = JsonSerializer.Serialize(logTypes);
                        await webView2.CoreWebView2.ExecuteScriptAsync($"updateLogTypesFromCSharp({JsonSerializer.Serialize(json)})");
                    }
                    // Xử lý các message từ tab Logs
                    else if (msg["action"].ToString() == "get_logs")
                    {
                        if (msg.ContainsKey("logType") && msg.ContainsKey("startTime") && msg.ContainsKey("endTime"))
                        {
                            string logType = msg["logType"].ToString();
                            DateTime startTime = DateTime.Parse(msg["startTime"].ToString());
                            DateTime endTime = DateTime.Parse(msg["endTime"].ToString());
                            
                            var logs = await GetLogsFromFile(logType, startTime, endTime);
                            string json = JsonSerializer.Serialize(logs);
                            await webView2.CoreWebView2.ExecuteScriptAsync($"updateLogsFromCSharp({JsonSerializer.Serialize(json)})");
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
                        await webView2.CoreWebView2.ExecuteScriptAsync($"updateFluentBitStatus({JsonSerializer.Serialize(json)})");
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xử lý message: " + ex.Message);
            }
        }

        private void UpdateOutputBlockByTag(string tag, bool enable)
        {
            var lines = File.ReadAllLines("fluent-bit.conf").ToList();
            // Xóa tất cả block OUTPUT/OUTPUTS cho tag này
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
            // Nếu enable thì thêm lại block OUTPUTS cho tag này (chỉ khi chưa tồn tại)
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
                    var block = new List<string>
                    {
                        "[OUTPUT]", // Dùng đúng format file hiện tại
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
            File.WriteAllLines("fluent-bit.conf", lines);
            // Có thể gọi FluentBitHelper.RestartFluentBitWithNotify(); nếu muốn restart dịch vụ
        }

        private void AddInputBlockDynamic(Dictionary<string, string> data)
        {
            var lines = File.ReadAllLines("fluent-bit.conf").ToList();
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
            File.WriteAllLines("fluent-bit.conf", lines);
        }

        private async System.Threading.Tasks.Task ReloadAndSendCollectors()
        {
            var collectors = new List<object>();
            var lines = File.ReadAllLines("fluent-bit.conf");
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
                    if (string.IsNullOrEmpty(name)) name = type;
                    if (string.IsNullOrEmpty(tag)) tag = name;
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
                                break;
                            }
                        }
                    }
                    collectors.Add(new
                    {
                        name,
                        type,
                        tag,
                        status = isEnabled ? "running" : "stopped"
                    });
                }
            }
            string json = JsonSerializer.Serialize(collectors);
            await webView2.CoreWebView2.ExecuteScriptAsync($"updateCollectorsFromCSharp({JsonSerializer.Serialize(json)})");
        }

        private List<Dictionary<string, string>> ReadParsersFromFile()
        {
            var result = new List<Dictionary<string, string>>();
            if (!File.Exists("parsers.conf")) return result;
            var lines = File.ReadAllLines("parsers.conf");
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
            File.AppendAllLines("parsers.conf", block);
        }

        private void EditParserBlock(int idx, Dictionary<string, string> data)
        {
            var lines = File.ReadAllLines("parsers.conf").ToList();
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
                File.WriteAllLines("parsers.conf", lines);
            }
        }

        private void DeleteParserBlock(int idx)
        {
            var lines = File.ReadAllLines("parsers.conf").ToList();
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
                File.WriteAllLines("parsers.conf", lines);
            }
        }

        // Lấy danh sách log types từ fluent-bit.conf
        private List<object> GetLogTypesFromConfig()
        {
            var logTypes = new List<object>();
            try
            {
                if (!File.Exists("fluent-bit.conf")) return logTypes;

                var lines = File.ReadAllLines("fluent-bit.conf");
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
        private async System.Threading.Tasks.Task<List<string>> GetLogsFromFile(string logType, DateTime startTime, DateTime endTime)
        {
            var logs = new List<string>();
            try
            {
                string logFilePath = Path.Combine("logs", $"{logType}.log");
                if (File.Exists(logFilePath))
                {
                    // Đọc toàn bộ file như một string thay vì từng dòng
                    string fileContent = await File.ReadAllTextAsync(logFilePath);
                    Console.WriteLine($"Đọc được {fileContent.Length} ký tự từ file {logFilePath}");
                    
                    // Tách thành các dòng và xử lý
                    var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine($"Tách thành {lines.Length} dòng");
                    
                    // Biến để tích lũy JSON fragments
                    var jsonBuffer = new List<string>();
                    var completeJsonObjects = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        // Thêm dòng vào buffer
                        jsonBuffer.Add(line);
                        
                        // Thử parse toàn bộ buffer như một JSON object
                        try
                        {
                            string combinedJson = string.Join("", jsonBuffer);
                            
                            // Kiểm tra xem có phải là JSON object hoàn chỉnh không
                            if (combinedJson.Trim().StartsWith("{") && combinedJson.Trim().EndsWith("}"))
                            {
                                // Thử parse để kiểm tra tính hợp lệ
                                using var doc = JsonDocument.Parse(combinedJson);
                                
                                // Nếu parse thành công, đây là JSON object hoàn chỉnh
                                completeJsonObjects.Add(combinedJson);
                                jsonBuffer.Clear();
                            }
                            else if (combinedJson.Trim().StartsWith("[") && combinedJson.Trim().EndsWith("]"))
                            {
                                // Nếu là JSON array, thử parse từng object trong array
                                using var doc = JsonDocument.Parse(combinedJson);
                                var root = doc.RootElement;
                                
                                if (root.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var element in root.EnumerateArray())
                                    {
                                        completeJsonObjects.Add(element.GetRawText());
                                    }
                                }
                                jsonBuffer.Clear();
                            }
                        }
                        catch (JsonException)
                        {
                            // Nếu parse thất bại, có thể là JSON fragment chưa hoàn chỉnh
                            // Tiếp tục tích lũy trong buffer
                            continue;
                        }
                    }
                    
                    // Xử lý các JSON objects hoàn chỉnh
                    foreach (var jsonStr in completeJsonObjects)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(jsonStr);
                            var root = doc.RootElement;
                            
                            DateTime logTime = DateTime.MinValue;
                            bool shouldInclude = false;
                            
                            if (logType == "winlog" && root.TryGetProperty("TimeGenerated", out var timeGen))
                            {
                                string timeStr = timeGen.GetString();
                                // Xử lý format thời gian có timezone
                                if (timeStr.Contains("+0700"))
                                {
                                    timeStr = timeStr.Replace(" +0700", "");
                                }
                                
                                if (DateTime.TryParse(timeStr, out logTime))
                                {
                                    shouldInclude = (logTime >= startTime && logTime <= endTime);
                                }
                                else
                                {
                                    // Nếu không parse được timestamp, vẫn thêm vào
                                    shouldInclude = true;
                                }
                            }
                            else if (logType == "syslog" && root.TryGetProperty("timestamp", out var timestamp))
                            {
                                if (DateTime.TryParse(timestamp.GetString(), out logTime))
                                {
                                    shouldInclude = (logTime >= startTime && logTime <= endTime);
                                }
                                else
                                {
                                    shouldInclude = true;
                                }
                            }
                            else if (logType == "win_stat")
                            {
                                // win_stat không có timestamp cụ thể, lấy tất cả
                                shouldInclude = true;
                            }
                            else
                            {
                                // Nếu không có timestamp field, vẫn thêm vào
                                shouldInclude = true;
                            }
                            
                            if (shouldInclude)
                            {
                                logs.Add(jsonStr);
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Lỗi parse JSON object: {ex.Message}");
                            Console.WriteLine($"JSON content: {jsonStr}");
                            // Vẫn thêm vào để hiển thị lỗi trong UI
                            logs.Add(jsonStr);
                        }
                    }
                    
                    // Nếu còn JSON fragments trong buffer, thêm vào để hiển thị lỗi
                    if (jsonBuffer.Count > 0)
                    {
                        string fragment = string.Join("", jsonBuffer);
                        Console.WriteLine($"Phát hiện JSON fragment: {fragment}");
                        logs.Add(fragment);
                    }
                }
                
                // Nếu không có dữ liệu từ file, thử dùng LogManagementService
                if (logs.Count == 0 && _logManagementService != null)
                {
                    var logLines = await _logManagementService.GetLogsAsync(logType, startTime, endTime);
                    if (logLines != null)
                    {
                        logs.AddRange(logLines);
                    }
                }
                
                // Debug: Log số lượng log tìm được
                Console.WriteLine($"Tìm thấy {logs.Count} log entries cho {logType} từ {startTime} đến {endTime}");
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc log: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
} 