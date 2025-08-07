using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class MiniWatchdogAgent
{
    static string mainConf = "fluent-bit.conf";
    static string backupConf = "fluent-bit2.conf";
    static string agentConfig = "agent_config.json";
    static string fluentBitExe = "fluent-bit"; // hoặc "fluent-bit.exe" trên Windows

    // Biến lưu config toàn cục và thời gian reload
    static AgentConfig GlobalConfig;
    static DateTime LastConfigReload = DateTime.MinValue;
    static int ConfigReloadIntervalSeconds = 300; // 5 phút

    public static async Task Main(string[] args)
    {
        while (true)
        {
            ReloadConfigIfNeeded();
            // 1. Kiểm tra file cấu hình chính
            if (!File.Exists(mainConf) || !IsValidConf(mainConf) || !StartFluentBit(mainConf))
            {
                Console.WriteLine("Lỗi cấu hình hoặc không thể khởi động fluent-bit với file chính!");

                // 2. Thử dùng file backup
                if (File.Exists(backupConf) && StartFluentBit(backupConf))
                {
                    Console.WriteLine("Đã khởi động fluent-bit với file backup!");
                    await SendErrorLog("Khởi động bằng file backup do lỗi cấu hình hoặc lỗi khởi động fluent-bit.", "BackupStart", backupConf);
                }
                else
                {
                    Console.WriteLine("Không thể khởi động fluent-bit với cả file backup!");
                    await SendErrorLog("Lỗi nghiêm trọng: Không thể khởi động fluent-bit với cả file chính và backup.", "CriticalError", backupConf);
                }
            }
            else
            {
                Console.WriteLine("Fluent-bit đã khởi động thành công với file chính.");
            }
            await Task.Delay(10000); // Lặp lại sau 10 giây
        }
    }

    static void ReloadConfigIfNeeded()
    {
        if ((DateTime.Now - LastConfigReload).TotalSeconds > ConfigReloadIntervalSeconds || GlobalConfig == null)
        {
            try
            {
                GlobalConfig = JsonSerializer.Deserialize<AgentConfig>(File.ReadAllText(agentConfig));
                LastConfigReload = DateTime.Now;
                Console.WriteLine($"Đã reload lại config lúc {LastConfigReload:HH:mm:ss dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi reload config: " + ex.Message);
            }
        }
    }

    static bool IsValidConf(string confPath)
    {
        // Đơn giản: kiểm tra file có nội dung, có thể mở rộng kiểm tra định dạng
        try
        {
            var content = File.ReadAllText(confPath);
            return !string.IsNullOrWhiteSpace(content);
        }
        catch
        {
            return false;
        }
    }

    static bool StartFluentBit(string confPath)
    {
        try
        {
            // Dừng fluent-bit cũ nếu đang chạy (tuỳ hệ điều hành, có thể cần quyền admin)
            KillProcess("fluent-bit");

            // Khởi động fluent-bit với file cấu hình chỉ định
            var psi = new ProcessStartInfo
            {
                FileName = fluentBitExe,
                Arguments = $"-c {confPath}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);

            // Đợi vài giây kiểm tra process có thực sự chạy không
            Task.Delay(3000).Wait();
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    static void KillProcess(string processName)
    {
        foreach (var proc in Process.GetProcessesByName(processName))
        {
            try { proc.Kill(); } catch { }
        }
    }

    static async Task SendErrorLog(string errorMsg, string errorType, string confFile)
    {
        try
        {
            var config = GlobalConfig;
            var agent = config?.agent_info;
            var serverUrl = config?.config_sync?.url ?? "http://localhost:5000/log";
            var token = config?.config_sync?.token ?? "";
            string localIP = GetLocalIPAddress();

            using var client = new HttpClient();
            // Thêm token vào header nếu có
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
            Console.WriteLine("Gửi log lỗi thất bại: " + ex.Message);
        }
    }

    static string GetLocalIPAddress()
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

    class AgentConfig
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