using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using SIEM_Agent.Core.Services;
using System.Threading.Tasks;
using Guna.UI2.WinForms;
using System.IO;
using System.Diagnostics;
using SIEM_Agent.Core;
using System.Text.Json;
using SIEM_Agent.Core.Config;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class LogsControl : UserControl
    {
        private readonly LogManagementService _logManagementService;
        private readonly Guna2DataGridView gridLogs;
        private readonly Guna2Button btnRefresh;
        private readonly Guna2Button btnClear;
        private readonly Guna2Button btnToggleCollect;
        private readonly Guna2DateTimePicker dtpStart;
        private readonly Guna2DateTimePicker dtpEnd;
        private readonly Guna2ComboBox cmbLogType;
        private bool isCollecting = true;
        private readonly Dictionary<string, string> nameMap = new(); // map tên hiển thị -> name gốc
        private readonly FluentBitConfig fluentBitConfig;
        private List<Dictionary<string, string>> inputBlocks = new(); // Lưu lại các block input thực tế
        private readonly Dictionary<string, string> displayToTagMap = new(); // map tên hiển thị -> tag

        public LogsControl(LogManagementService logManagementService)
        {
            _logManagementService = logManagementService ?? throw new ArgumentNullException(nameof(logManagementService));
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.Dock = DockStyle.Fill;

            fluentBitConfig = new FluentBitConfig("fluent-bit.conf");

            // Panel chứa các control
            var panel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                FillColor = Color.FromArgb(28, 28, 28),
                BorderRadius = 10,
                Margin = new Padding(10)
            };

            // ComboBox chọn loại log
            cmbLogType = new Guna2ComboBox
            {
                Location = new Point(10, 10),
                Size = new Size(250, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            LoadLogTypesFromInputBlocks();
            cmbLogType.SelectedIndexChanged += CmbLogType_SelectedIndexChanged;

            // DateTimePicker
            dtpStart = new Guna2DateTimePicker
            {
                Location = new Point(270, 10),
                Size = new Size(180, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                ShowUpDown = true,
                Value = DateTime.Now.AddDays(-1)
            };

            dtpEnd = new Guna2DateTimePicker
            {
                Location = new Point(460, 10),
                Size = new Size(180, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                ShowUpDown = true,
                Value = DateTime.Now
            };

            // Nút Refresh
            btnRefresh = new Guna2Button
            {
                Text = "Làm mới",
                Location = new Point(650, 10),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnRefresh.Click += BtnRefresh_Click;

            // Nút Clear
            btnClear = new Guna2Button
            {
                Text = "Xóa log",
                Location = new Point(760, 10),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnClear.Click += BtnClear_Click;

            // Nút Toggle thu thập log
            btnToggleCollect = new Guna2Button
            {
                Text = "Dừng lấy log",
                Location = new Point(870, 10),
                Size = new Size(120, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 180, 80),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnToggleCollect.Click += (s, e) =>
            {
                if (isCollecting)
                {
                    FluentBitHelper.StopFluentBit();
                    btnToggleCollect.Text = "Bắt đầu lấy log";
                    btnToggleCollect.FillColor = Color.FromArgb(200, 60, 60);
                    isCollecting = false;
                }
                else
                {
                    FluentBitHelper.StartFluentBit();
                    btnToggleCollect.Text = "Dừng lấy log";
                    btnToggleCollect.FillColor = Color.FromArgb(60, 180, 80);
                    isCollecting = true;
                }
            };

            // Grid hiển thị logs
            gridLogs = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            };

            // Cấu hình DataGridView
            gridLogs.BackgroundColor = Color.FromArgb(32, 32, 32);
            gridLogs.BorderStyle = BorderStyle.None;
            gridLogs.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            gridLogs.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            gridLogs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(32, 32, 32);
            gridLogs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridLogs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            gridLogs.DefaultCellStyle.BackColor = Color.FromArgb(32, 32, 32);
            gridLogs.DefaultCellStyle.ForeColor = Color.White;
            gridLogs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            gridLogs.DefaultCellStyle.SelectionForeColor = Color.White;
            gridLogs.EnableHeadersVisualStyles = false;
            gridLogs.GridColor = Color.FromArgb(64, 64, 64);
            gridLogs.RowHeadersVisible = false;
            gridLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridLogs.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            gridLogs.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            gridLogs.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(32, 32, 32);
            gridLogs.RowHeadersDefaultCellStyle.ForeColor = Color.White;
            gridLogs.RowHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            gridLogs.RowHeadersDefaultCellStyle.SelectionForeColor = Color.White;

            // Thêm các control vào panel
            panel.Controls.Add(cmbLogType);
            panel.Controls.Add(dtpStart);
            panel.Controls.Add(dtpEnd);
            panel.Controls.Add(btnRefresh);
            panel.Controls.Add(btnClear);
            panel.Controls.Add(btnToggleCollect);

            // Thêm panel và grid vào control
            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.RowCount = 2;
            table.ColumnCount = 1;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // Panel cao 100px
            // Dòng dưới chiếm toàn bộ phần còn lại
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.Controls.Add(panel, 0, 0);
            table.Controls.Add(gridLogs, 0, 1);
            this.Controls.Add(table);

            // Load dữ liệu ban đầu
            LoadLogs();
        }

        private void CmbLogType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLogs();
        }

        private void LoadLogTypesFromInputBlocks()
        {
            cmbLogType.Items.Clear();
            displayToTagMap.Clear();
            inputBlocks = fluentBitConfig.GetAllInputBlocks();
            var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"winlog", "Windows Event Log"},
                {"syslog", "Syslog"},
                {"odbc", "ODBC Database"},
                {"jdbc", "JDBC Database"},
                {"ftp", "FTP"},
                // Thêm các loại khác nếu muốn
            };
            foreach (var block in inputBlocks)
            {
                if (!block.ContainsKey("Name")) continue;
                string name = block["Name"];
                string display = friendlyNames.ContainsKey(name) ? friendlyNames[name] : name;
                if (block.ContainsKey("Tag"))
                    display += $" [Tag={block["Tag"]}]";
                cmbLogType.Items.Add(display);
                displayToTagMap[display] = block.ContainsKey("Tag") ? block["Tag"] : null;
            }
            if (cmbLogType.Items.Count > 0)
                cmbLogType.SelectedIndex = 0;
        }

        private string GetSelectedLogType()
        {
            var selected = cmbLogType.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected) || inputBlocks.Count == 0) return "winlog";
            // Tìm block tương ứng
            int idx = cmbLogType.SelectedIndex;
            if (idx >= 0 && idx < inputBlocks.Count)
            {
                var block = inputBlocks[idx];
                // Ưu tiên lấy tag nếu có
                if (block.ContainsKey("Tag"))
                    return block["Tag"];
                // Nếu không có tag, trả về name
                if (block.ContainsKey("Name"))
                    return block["Name"];
            }
            return selected.ToLower();
        }

        private async void LoadLogs()
        {
            try
            {
                if (_logManagementService == null)
                {
                    MessageBox.Show("LogManagementService chưa được khởi tạo!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                gridLogs.Rows.Clear();
                string logType = GetSelectedLogType();

                // Đảm bảo cột đúng loại log
                gridLogs.Columns.Clear();
                if (logType == "winlog")
                {
                    gridLogs.Columns.Add("STT", "STT");
                    gridLogs.Columns.Add("Thời gian", "Thời gian");
                    gridLogs.Columns.Add("Nguồn", "Nguồn");
                    gridLogs.Columns.Add("Cấp độ", "Cấp độ");
                    gridLogs.Columns.Add("Sự kiện", "Sự kiện");
                    gridLogs.Columns.Add("Mô tả", "Mô tả");
                }
                else if (logType == "syslog")
                {
                    gridLogs.Columns.Add("STT", "STT");
                    gridLogs.Columns.Add("Thời gian", "Thời gian");
                    gridLogs.Columns.Add("Host", "Host");
                    gridLogs.Columns.Add("Facility", "Facility");
                    gridLogs.Columns.Add("Severity", "Severity");
                    gridLogs.Columns.Add("Message", "Message");
                }
                else if (logType == "win_stat")
                {
                    gridLogs.Columns.Add("STT", "STT");
                    gridLogs.Columns.Add("Uptime", "Uptime");
                    gridLogs.Columns.Add("CPU (%)", "CPU (%)");
                    gridLogs.Columns.Add("Processes", "Processes");
                    gridLogs.Columns.Add("Threads", "Threads");
                    gridLogs.Columns.Add("Handles", "Handles");
                    gridLogs.Columns.Add("RAM Used (MB)", "RAM Used (MB)");
                    gridLogs.Columns.Add("RAM Total (MB)", "RAM Total (MB)");
                }
                // ... có thể bổ sung các loại log khác nếu muốn

                // Cấu hình tự động co giãn cột
                gridLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                if (gridLogs.Columns.Count > 0)
                    gridLogs.Columns[gridLogs.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                var logs = (logType == "winstat")
                    ? await _logManagementService.GetLogsAsync(logType, DateTime.MinValue, DateTime.MaxValue) ?? new List<string>()
                    : await _logManagementService.GetLogsAsync(logType, dtpStart.Value, dtpEnd.Value) ?? new List<string>();
                int stt = 1;
                foreach (var log in logs)
                {
                    if (string.IsNullOrWhiteSpace(log)) continue;
                    try
                    {
                        using var doc = JsonDocument.Parse(log);
                        var root = doc.RootElement;
                        if (logType == "winlog")
                        {
                            var timeGenerated = root.GetProperty("TimeGenerated").GetString();
                            var source = root.GetProperty("SourceName").GetString();
                            var level = root.GetProperty("EventType").GetString();
                            var eventId = root.GetProperty("EventID").GetInt32().ToString();
                            var message = root.GetProperty("Message").GetString();
                            gridLogs.Rows.Add(stt++, timeGenerated, source, level, eventId, message);
                        }
                        else if (logType == "syslog")
                        {
                            var timestamp = root.GetProperty("timestamp").GetString();
                            var host = root.GetProperty("host").GetString();
                            var facility = root.GetProperty("facility").GetString();
                            var severity = root.GetProperty("severity").GetString();
                            var message = root.GetProperty("message").GetString();
                            gridLogs.Rows.Add(stt++, timestamp, host, facility, severity, message);
                        }
                        else if (logType == "win_stat")
                        {
                            var uptime = root.TryGetProperty("uptime_human", out var up) ? up.GetString() : "";
                            var cpu = root.TryGetProperty("cpu_utilization", out var cpuVal) ? cpuVal.GetDouble() : 0;
                            var processes = root.TryGetProperty("processes", out var p) ? p.GetInt32() : 0;
                            var threads = root.TryGetProperty("threads", out var t) ? t.GetInt32() : 0;
                            var handles = root.TryGetProperty("handles", out var h) ? h.GetInt32() : 0;
                            var ramUsed = root.TryGetProperty("physical_used", out var ru) ? ru.GetInt32() / 1024 / 1024 : 0;
                            var ramTotal = root.TryGetProperty("physical_total", out var rt) ? rt.GetInt32() / 1024 / 1024 : 0;
                            gridLogs.Rows.Add(stt++, uptime, cpu, processes, threads, handles, ramUsed, ramTotal);
                        }
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải log: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadLogs();
        }

        private async void BtnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn xóa tất cả log?", "Xác nhận", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    string logType = GetSelectedLogType();
                    await _logManagementService.ClearLogsAsync(logType);
                    LoadLogs();
                    MessageBox.Show("Đã xóa log thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa log: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
} 