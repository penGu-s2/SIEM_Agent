using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Timers;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SIEM_Agent
{
    public class Config
    {
        public string LogType { get; set; } = "File";
        public string LogPath { get; set; } = "";
        public string OpenSearchEndpoint { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CAFile { get; set; } = "";
        public string CertFile { get; set; } = "";
        public string KeyFile { get; set; } = "";
        public string Interval { get; set; } = "5";
    }

    public partial class Form1 : Form
    {
        private System.Timers.Timer timer;
        private Process fluentBitProcess;
        private string configPath = "agent_config.json";
        private string fluentBitConfPath = "fluent-bit.conf";

        public Form1()
        {
            InitializeComponent();
            CheckLogWritePermission();
            LoadConfig();
        }

        private void CheckLogWritePermission()
        {
            try
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");
                File.WriteAllText("logs/test_write.txt", "test");
                File.Delete("logs/test_write.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể ghi file vào thư mục logs! Hãy chạy ứng dụng bằng quyền Administrator.\n" + ex.Message, "Lỗi quyền ghi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            SaveConfig();
            MessageBox.Show("Đã lưu cấu hình!");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            SaveConfig();
            GenerateFluentBitConfig();
            StartFluentBit();
            int interval = int.Parse(cmbInterval.SelectedItem.ToString()) * 60 * 1000;
            timer = new System.Timers.Timer(interval);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            lblStatus.Text = "Đang chạy";
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopFluentBit();
            timer?.Stop();
            lblStatus.Text = "Đã dừng";
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StopFluentBit();
            GenerateFluentBitConfig();
            StartFluentBit();
        }

        private void StartFluentBit()
        {
            try
            {
                fluentBitProcess = new Process();
                fluentBitProcess.StartInfo.FileName = @"C:\Program Files\fluent-bit\bin\fluent-bit.exe";
                fluentBitProcess.StartInfo.Arguments = $"-c {fluentBitConfPath}";
                fluentBitProcess.StartInfo.CreateNoWindow = true;
                fluentBitProcess.StartInfo.UseShellExecute = false;
                fluentBitProcess.Start();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi: " + ex.Message;
            }
        }

        private void StopFluentBit()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("fluent-bit"))
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi: " + ex.Message;
            }
        }

        private void GenerateFluentBitConfig()
        {
            var config = new
            {
                LogType = cmbLogType.SelectedItem?.ToString() ?? "File",
                LogPath = txtLogPath.Text,
                OpenSearchEndpoint = txtOpenSearchEndpoint.Text,
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                CAFile = txtCAFile.Text,
                CertFile = txtCertFile.Text,
                KeyFile = txtKeyFile.Text
            };

            string inputSection;
            if (config.LogType == "WinlogEvent")
            {
                inputSection = $@"
[INPUT]
    Name         winlog
    Tag          winlog
    Channels     Application,System,Security
    DB           C:/Program Files/fluent-bit/logs/winlog.db
    Refresh_Interval 5
";
            }
            else // Mặc định là File
            {
                inputSection = $@"
[INPUT]
    Name         tail
    Path         {config.LogPath}
    Tag          app.log
    DB           C:/Program Files/fluent-bit/logs/flb.db
    Refresh_Interval 5
";
            }

            string conf = $@"
[SERVICE]
    Flush        1
    Daemon       Off
    Log_Level    info

{inputSection}

[FILTER]
    Name         record_modifier
    Match        *
    Record       hostname ${{COMPUTERNAME}}

[OUTPUT]
    Name         opensearch
    Match        *
    Host         {config.OpenSearchEndpoint}
    Port         9200
    HTTP_User    {config.Username}
    HTTP_Passwd  {config.Password}
    Index        fluentbit-logs
    Type         _doc
    tls          On
    tls.verify   On
    tls.ca_file  {config.CAFile}
    tls.crt_file {config.CertFile}
    tls.key_file {config.KeyFile}
    Suppress_Type_Name On
    Logstash_Format On
";
            File.WriteAllText(fluentBitConfPath, conf);
        }

        private void SaveConfig()
        {
            var config = new Config
            {
                LogType = cmbLogType.SelectedItem?.ToString() ?? "File",
                LogPath = txtLogPath.Text,
                OpenSearchEndpoint = txtOpenSearchEndpoint.Text,
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                CAFile = txtCAFile.Text,
                CertFile = txtCertFile.Text,
                KeyFile = txtKeyFile.Text,
                Interval = cmbInterval.SelectedItem?.ToString() ?? "5"
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, jsonString);
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string jsonString = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<Config>(jsonString);

                    if (config != null)
                    {
                        cmbLogType.SelectedItem = config.LogType;
                        txtLogPath.Text = config.LogPath;
                        txtOpenSearchEndpoint.Text = config.OpenSearchEndpoint;
                        txtUsername.Text = config.Username;
                        txtPassword.Text = config.Password;
                        txtCAFile.Text = config.CAFile;
                        txtCertFile.Text = config.CertFile;
                        txtKeyFile.Text = config.KeyFile;
                        cmbInterval.SelectedItem = config.Interval;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi đọc file cấu hình: {ex.Message}");
                }
            }
        }

        private async void btnViewLog_Click(object sender, EventArgs e)
        {
            // Bỏ qua xác thực SSL tự ký (chỉ dùng cho dev/test)
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;

            string endpoint = txtOpenSearchEndpoint.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string url = $"https://{endpoint}:9200/fluentbit-logs*/_search";
            string query = @"{\n  ""query"": { ""match_all"": {} },\n  ""size"": 10,\n  ""sort"": [{ ""@timestamp"": { ""order"": ""desc"" } }]\n}";

            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var byteArray = System.Text.Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");
                try
                {
                    var response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();
                    txtLogResult.Text = result;
                }
                catch (Exception ex)
                {
                    txtLogResult.Text = "Lỗi: " + ex.Message;
                }
            }
        }
    }
}
