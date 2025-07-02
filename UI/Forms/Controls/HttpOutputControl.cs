using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIEM_Agent.Core.Config;
using SIEM_Agent.Core;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class HttpOutputControl : UserControl
    {
        private CheckedListBox checkedListBox;
        private Dictionary<string, Panel> configPanels = new Dictionary<string, Panel>();
        private Dictionary<string, Dictionary<string, string>> configValues = new Dictionary<string, Dictionary<string, string>>();
        private FluentBitConfig fluentBitConfig;
        private List<string> inputNames;
        private Button btnSave;
        private ListView listViewOutputs;
        private Button btnDelete;
        private bool _suppressPopup = false;

        public HttpOutputControl()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(32, 32, 32);
            fluentBitConfig = new FluentBitConfig("fluent-bit.conf");
            LoadHttpConfigsFromFile(); // Đọc configValues từ file khi khởi tạo

            // Lấy danh sách inputNames là hợp của các input từ FluentBitConfig và các Match đã có trong configValues
            var inputSet = new HashSet<string>(fluentBitConfig.GetAllInputNames());
            foreach (var kv in configValues)
                inputSet.Add(kv.Key);
            inputNames = inputSet.ToList();

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(32, 32, 32),
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            // Bên trái: checkedListBox + panel cấu hình động
            var lblTitle = new Label
            {
                Text = "Cấu hình gửi log qua HTTP cho từng nguồn dữ liệu",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.LightSkyBlue,
                Dock = DockStyle.Fill,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 10, 0, 0)
            };
            checkedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                CheckOnClick = true
            };
            // Luôn clear và add lại checkedListBox dựa trên configValues
            checkedListBox.Items.Clear();
            foreach (var input in inputNames)
            {
                checkedListBox.Items.Add(input, configValues.ContainsKey(input));
            }
            checkedListBox.ItemCheck += (s, e) =>
            {
                if (_suppressPopup) return;

                string input = checkedListBox.Items[e.Index].ToString();

                if (e.NewValue == CheckState.Checked)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        Dictionary<string, string> oldCfg = configValues.ContainsKey(input) ? configValues[input] : null;
                        using (var popup = new HttpConfigPopupForm(input, oldCfg))
                        {
                            var result = popup.ShowDialog();
                            if (result == DialogResult.OK && popup.ResultConfig != null)
                            {
                                configValues[input] = popup.ResultConfig;
                                SaveHttpOutputs();
                            }
                            else
                            {
                                _suppressPopup = true;
                                checkedListBox.SetItemChecked(e.Index, false);
                                _suppressPopup = false;
                            }
                        }
                    }));
                }
                else
                {
                    if (configValues.ContainsKey(input))
                        configValues.Remove(input);
                    this.BeginInvoke(new Action(() =>
                    {
                        SaveHttpOutputs();
                    }));
                }
            };
            // Sử dụng TableLayoutPanel để layout các thành phần bên trái (chỉ còn tiêu đề và checkedListBox)
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(32, 32, 32)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // lblTitle
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // checkedListBox
            leftLayout.Controls.Add(lblTitle, 0, 0);
            leftLayout.Controls.Add(checkedListBox, 0, 1);
            mainLayout.Controls.Add(leftLayout, 0, 0);

            // Bên phải: danh sách output HTTP đã cấu hình + nút xóa
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 32, 32) };
            listViewOutputs = new ListView
            {
                Dock = DockStyle.Top,
                Height = 250,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                BackColor = Color.FromArgb(36, 36, 36),
                ForeColor = Color.White
            };
            listViewOutputs.Columns.Add("Nguồn", 100);
            listViewOutputs.Columns.Add("Host", 120);
            listViewOutputs.Columns.Add("Port", 60);
            listViewOutputs.Columns.Add("URI", 120);
            listViewOutputs.Columns.Add("Format", 80);
            listViewOutputs.Columns.Add("Json_date_key", 100);
            listViewOutputs.Columns.Add("Json_date_format", 100);
            btnDelete = new Button
            {
                Text = "Xóa output đã chọn",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.IndianRed,
                ForeColor = Color.White
            };
            btnDelete.Click += (s, e) => DeleteSelectedOutput();
            rightPanel.Controls.Add(btnDelete);
            rightPanel.Controls.Add(listViewOutputs);

            mainLayout.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainLayout);

            LoadHttpOutputsToListView();
        }

        private void LoadHttpConfigsFromFile()
        {
            configValues.Clear();
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf");
            Dictionary<string, string> block = null;
            foreach (var line in lines)
            {
                if (line.Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    if (block != null && block.ContainsKey("Name") && block["Name"].Trim().Equals("http", StringComparison.OrdinalIgnoreCase))
                    {
                        string input = block.ContainsKey("Match") ? block["Match"].Trim() : "";
                        if (!string.IsNullOrEmpty(input))
                            configValues[input] = new Dictionary<string, string>(block);
                    }
                    block = new Dictionary<string, string>();
                }
                else if (block != null && line.Trim().Length > 0)
                {
                    var idx = line.IndexOf(' ');
                    if (idx > 0)
                    {
                        var key = line.Substring(0, idx).Trim();
                        var value = line.Substring(idx).Trim();
                        block[key] = value;
                    }
                }
            }
            // Block cuối cùng
            if (block != null && block.ContainsKey("Name") && block["Name"].Trim().Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                string input = block.ContainsKey("Match") ? block["Match"].Trim() : "";
                if (!string.IsNullOrEmpty(input))
                    configValues[input] = new Dictionary<string, string>(block);
            }
        }

        private Panel CreateHttpConfigPanel(string input)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(36, 36, 36),
                Padding = new Padding(10)
            };
            var lblHost = new Label { Text = "Host", ForeColor = Color.White, AutoSize = true };
            var txtHost = new TextBox { Name = "txtHost", Width = 120 };
            var lblPort = new Label { Text = "Port", ForeColor = Color.White, AutoSize = true };
            var txtPort = new TextBox { Name = "txtPort", Width = 60 };
            var lblUri = new Label { Text = "URI", ForeColor = Color.White, AutoSize = true };
            var txtUri = new TextBox { Name = "txtUri", Width = 120 };
            var lblFormat = new Label { Text = "Format", ForeColor = Color.White, AutoSize = true };
            var txtFormat = new TextBox { Name = "txtFormat", Width = 80, Text = "json" };
            var lblJsonDateKey = new Label { Text = "Json_date_key", ForeColor = Color.White, AutoSize = true };
            var txtJsonDateKey = new TextBox { Name = "txtJsonDateKey", Width = 80, Text = "timestamp" };
            var lblJsonDateFormat = new Label { Text = "Json_date_format", ForeColor = Color.White, AutoSize = true };
            var txtJsonDateFormat = new TextBox { Name = "txtJsonDateFormat", Width = 80, Text = "iso8601" };

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            layout.Controls.AddRange(new Control[] {
                lblHost, txtHost, lblPort, txtPort, lblUri, txtUri, lblFormat, txtFormat, lblJsonDateKey, txtJsonDateKey, lblJsonDateFormat, txtJsonDateFormat
            });
            panel.Controls.Add(layout);

            // Lưu giá trị khi thay đổi
            txtHost.TextChanged += (s, e) => SetConfigValue(input, "Host", txtHost.Text);
            txtPort.TextChanged += (s, e) => SetConfigValue(input, "Port", txtPort.Text);
            txtUri.TextChanged += (s, e) => SetConfigValue(input, "URI", txtUri.Text);
            txtFormat.TextChanged += (s, e) => SetConfigValue(input, "Format", txtFormat.Text);
            txtJsonDateKey.TextChanged += (s, e) => SetConfigValue(input, "Json_date_key", txtJsonDateKey.Text);
            txtJsonDateFormat.TextChanged += (s, e) => SetConfigValue(input, "Json_date_format", txtJsonDateFormat.Text);

            // Nếu đã có config cũ thì điền lại
            if (configValues.ContainsKey(input))
            {
                var cfg = configValues[input];
                txtHost.Text = cfg.ContainsKey("Host") ? cfg["Host"] : "";
                txtPort.Text = cfg.ContainsKey("Port") ? cfg["Port"] : "";
                txtUri.Text = cfg.ContainsKey("URI") ? cfg["URI"] : "";
                txtFormat.Text = cfg.ContainsKey("Format") ? cfg["Format"] : "json";
                txtJsonDateKey.Text = cfg.ContainsKey("Json_date_key") ? cfg["Json_date_key"] : "timestamp";
                txtJsonDateFormat.Text = cfg.ContainsKey("Json_date_format") ? cfg["Json_date_format"] : "iso8601";
            }

            return panel;
        }

        private void SetConfigValue(string input, string key, string value)
        {
            if (!configValues.ContainsKey(input))
                configValues[input] = new Dictionary<string, string>();
            configValues[input][key] = value;
        }

        private void SaveHttpOutputs()
        {
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf").ToList();
            // Xóa block [OUTPUT] http của các nguồn đang bật (không xóa hết)
            var enabledInputs = inputNames.Where(input => checkedListBox.CheckedItems.Contains(input)).ToHashSet();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int start = i;
                    int end = i + 1;
                    while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                    bool isHttp = false;
                    string match = null;
                    for (int j = start + 1; j < end; j++)
                    {
                        if (lines[j].Trim().StartsWith("Name") && lines[j].ToLower().Contains("http"))
                            isHttp = true;
                        if (lines[j].Trim().StartsWith("Match"))
                            match = lines[j].Trim().Substring(5).Trim();
                    }
                    if (isHttp && enabledInputs.Contains(match))
                        lines.RemoveRange(start, end - start);
                }
            }
            // Thêm lại các block [OUTPUT] http cho từng nguồn được bật
            foreach (var input in enabledInputs)
            {
                var cfg = configValues.ContainsKey(input) ? configValues[input] : new Dictionary<string, string>();
                var block = new List<string>
                {
                    "[OUTPUT]",
                    "    Name  http",
                    $"    Match {input}",
                    $"    Host  {(cfg.ContainsKey("Host") ? cfg["Host"] : "")}",
                    $"    Port  {(cfg.ContainsKey("Port") ? cfg["Port"] : "")}",
                    $"    URI   {(cfg.ContainsKey("URI") ? cfg["URI"] : "")}",
                    $"    Format {(cfg.ContainsKey("Format") ? cfg["Format"] : "json")}",
                    $"    Json_date_key {(cfg.ContainsKey("Json_date_key") ? cfg["Json_date_key"] : "timestamp")}",
                    $"    Json_date_format {(cfg.ContainsKey("Json_date_format") ? cfg["Json_date_format"] : "iso8601")}",
                    ""
                };
                lines.AddRange(block);
            }
            System.IO.File.WriteAllLines("fluent-bit.conf", lines);
            FluentBitHelper.RestartFluentBitWithNotify();
            LoadHttpConfigsFromFile();
            // Cập nhật lại checkedListBox sau khi lưu
            checkedListBox.Items.Clear();
            foreach (var input in inputNames)
            {
                checkedListBox.Items.Add(input, configValues.ContainsKey(input));
            }
            ReloadHttpListViewAfterChange();
            SIEM_Agent.Core.FluentBitHelper.RestartFluentBitWithNotify();
            MessageBox.Show("Đã lưu cấu hình HTTP thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadHttpOutputsToListView()
        {
            listViewOutputs.Items.Clear();
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf");
            Dictionary<string, string> block = new Dictionary<string, string>();
            int lineNumber = 0;
            foreach (var line in lines)
            {
                lineNumber++;
                if (line.Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    if (block.Count > 0 && block.ContainsKey("Name") && block["Name"].Trim().Equals("http", StringComparison.OrdinalIgnoreCase))
                    {
                        AddBlockToListView(block);
                    }
                    block = new Dictionary<string, string>();
                }
                else if (line.Trim().Length > 0)
                {
                    var trimmed = line.Trim();
                    var idx = trimmed.IndexOf(' ');
                    if (idx > 0)
                    {
                        var key = trimmed.Substring(0, idx).Trim();
                        var value = trimmed.Substring(idx).Trim();
                        block[key] = value;
                    }                    
                }
            }
            if (block.Count > 0 && block.ContainsKey("Name") && block["Name"].Trim().Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                AddBlockToListView(block);
            }
        }

        private void AddBlockToListView(Dictionary<string, string> block)
        {
            string input = block.ContainsKey("Match") ? block["Match"].Trim() : "";
            var lvi = new ListViewItem(new string[]
            {
                input,
                block.ContainsKey("Host") ? block["Host"].Trim() : "",
                block.ContainsKey("Port") ? block["Port"].Trim() : "",
                block.ContainsKey("URI") ? block["URI"].Trim() : "",
                block.ContainsKey("Format") ? block["Format"].Trim() : "",
                block.ContainsKey("Json_date_key") ? block["Json_date_key"].Trim() : "",
                block.ContainsKey("Json_date_format") ? block["Json_date_format"].Trim() : ""
            });
            listViewOutputs.Items.Add(lvi);
        }

        private void DeleteSelectedOutput()
        {
            if (listViewOutputs.SelectedItems.Count == 0) return;
            var input = listViewOutputs.SelectedItems[0].SubItems[0].Text;
            // Xóa block [OUTPUT] http có Match = input
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf").ToList();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int start = i;
                    int end = i + 1;
                    while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                    bool isHttp = false;
                    string match = null;
                    for (int j = start + 1; j < end; j++)
                    {
                        if (lines[j].Trim().StartsWith("Name") && lines[j].ToLower().Contains("http"))
                            isHttp = true;
                        if (lines[j].Trim().StartsWith("Match"))
                            match = lines[j].Trim().Substring(5).Trim();
                    }
                    if (isHttp && match == input)
                        lines.RemoveRange(start, end - start);
                }
            }
            System.IO.File.WriteAllLines("fluent-bit.conf", lines);
            LoadHttpConfigsFromFile();
            ReloadHttpListViewAfterChange();
            SIEM_Agent.Core.FluentBitHelper.RestartFluentBitWithNotify();
        }

        private void ReloadHttpListViewAfterChange()
        {
            LoadHttpOutputsToListView();
        }
    }
} 