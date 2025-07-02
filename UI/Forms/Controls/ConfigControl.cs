using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using SIEM_Agent.Core.Config;
using Guna.UI2.WinForms;
using SIEM_Agent.Core;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class ConfigControl : UserControl
    {
        private readonly FluentBitConfig _fluentBitConfig;
        private readonly Dictionary<string, string> _configValues;
        private readonly Dictionary<string, string> _originalConfigValues;
        private readonly Guna2Panel panelConfig;
        private readonly Guna2Button btnSave;
        private readonly Guna2Button btnReset;
        private readonly Guna2ComboBox cmbLogType;
        private readonly Guna2Button btnAddConfig;
        private string _lastAddedInputType = null;
        private Dictionary<string, string> _lastAddedInputValues = null;
        // Danh sách các input và các trường cấu hình cần thiết
        private readonly Dictionary<string, string[]> _inputRequiredFields = new Dictionary<string, string[]>
        {
            { "blob", new[] { "Path", "Tag" } },
            { "kubernetes_events", new[] { "Tag", "Kube_URL", "Kube_CA_File", "Kube_Token_File" } },
            { "kafka", new[] { "Tag", "Brokers", "Topics" } },
            { "fluentbit_metrics", new[] { "Tag", "Scrape_Interval" } },
            { "prometheus_scrape", new[] { "Tag", "Host", "Port", "Scrape_Interval", "Metrics_Path" } },
            { "tail", new[] { "Path", "Tag" } },
            { "dummy", new[] { "Tag", "Dummy", "Samples" } },
            { "http", new[] { "Tag", "Host", "Port" } },
            { "statsd", new[] { "Tag", "Host", "Port" } },
            { "opentelemetry", new[] { "Tag", "Host", "Port" } },
            { "elasticsearch", new[] { "Tag", "Host", "Port", "Path" } },
            { "splunk", new[] { "Tag", "Host", "Port", "Splunk_Token" } },
            { "prometheus_remote_write", new[] { "Tag", "Host", "Port" } },
            { "event_type", new[] { "Tag", "Event" } },
            { "nginx_metrics", new[] { "Tag", "Host", "Port", "Status_URL" } },
            { "winlog", new[] { "Tag", "Channels", "Interval_Sec", "DB" } },
            { "winstat", new[] { "Tag", "Interval_Sec" } },
            { "winevtlog", new[] { "Tag", "Channels" } },
            { "windows_exporter_metrics", new[] { "Tag", "Scrape_Interval" } },
            { "syslog", new[] { "Tag", "Listen", "Port", "Mode", "Parser" } },
        };

        public ConfigControl()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.Dock = DockStyle.Fill;

            _fluentBitConfig = new FluentBitConfig("fluent-bit.conf");
            _configValues = new Dictionary<string, string>();
            _originalConfigValues = new Dictionary<string, string>();

            // Panel chứa các control
            panelConfig = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.FromArgb(28, 28, 28),
                BorderRadius = 10,
                Margin = new Padding(10)
            };

            // ComboBox chọn loại log
            cmbLogType = new Guna2ComboBox
            {
                Location = new Point(10, 10),
                Size = new Size(200, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbLogType.Items.AddRange(new string[] { "Windows Event Log", "Syslog" });
            cmbLogType.SelectedIndexChanged += CmbLogType_SelectedIndexChanged;

            // Nút Save
            btnSave = new Guna2Button
            {
                Text = "Lưu",
                Location = new Point(220, 10),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnSave.Click += BtnSave_Click;

            // Nút Reset
            btnReset = new Guna2Button
            {
                Text = "Reset",
                Location = new Point(330, 10),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnReset.Click += BtnReset_Click;

            // Nút Thêm mới
            btnAddConfig = new Guna2Button
            {
                Text = "Thêm mới",
                Location = new Point(440, 10),
                Size = new Size(100, 36),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 200, 120),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnAddConfig.Click += BtnAddConfig_Click;

            // Thêm các control vào panel
            panelConfig.Controls.Add(cmbLogType);
            panelConfig.Controls.Add(btnSave);
            panelConfig.Controls.Add(btnReset);
            panelConfig.Controls.Add(btnAddConfig);

            // Thêm panel vào control
            this.Controls.Add(panelConfig);

            // Load cấu hình ban đầu
            LoadConfig();
        }

        private void CmbLogType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadConfig();
        }

        private void BtnAddConfig_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = "Thêm mới input FluentBit";
                form.Size = new Size(400, 300);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblInput = new Label { Text = "Chọn input:", Location = new Point(10, 20), AutoSize = true, ForeColor = Color.Black };
                var cmbInput = new ComboBox { Location = new Point(110, 18), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
                cmbInput.Items.AddRange(new List<string>(_inputRequiredFields.Keys).ToArray());
                cmbInput.SelectedIndex = 0;

                var panelFields = new Panel { Location = new Point(10, 60), Size = new Size(370, 140), AutoScroll = true };

                void CreateFields(string inputType)
                {
                    panelFields.Controls.Clear();
                    var fields = _inputRequiredFields[inputType];
                    int y = 0;
                    foreach (var field in fields)
                    {
                        var lbl = new Label { Text = field + ":", Location = new Point(0, y), AutoSize = true, ForeColor = Color.Black };
                        panelFields.Controls.Add(lbl);
                        if (field.Equals("Parser", StringComparison.OrdinalIgnoreCase))
                        {
                            var parserCombo = new ComboBox
                            {
                                Name = "txt" + field,
                                Location = new Point(120, y),
                                Width = 200,
                                DropDownStyle = ComboBoxStyle.DropDown,
                                BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
                                ForeColor = System.Drawing.Color.Black,
                                Font = new Font("Segoe UI", 10)
                            };
                            var parsers = GetParsersFromFile();
                            parserCombo.Items.AddRange(parsers.ToArray());
                            panelFields.Controls.Add(parserCombo);
                        }
                        else
                        {
                            var txt = new TextBox { Name = "txt" + field, Location = new Point(120, y), Width = 200 };
                            panelFields.Controls.Add(txt);
                        }
                        y += 35;
                    }
                }
                CreateFields(cmbInput.SelectedItem?.ToString() ?? "tail");
                cmbInput.SelectedIndexChanged += (s, e2) =>
                {
                    CreateFields(cmbInput.SelectedItem.ToString());
                };

                var btnOK = new Button { Text = "OK", Location = new Point(110, 220), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Hủy", Location = new Point(200, 220), DialogResult = DialogResult.Cancel };

                form.Controls.Add(lblInput);
                form.Controls.Add(cmbInput);
                form.Controls.Add(panelFields);
                form.Controls.Add(btnOK);
                form.Controls.Add(btnCancel);

                form.AcceptButton = btnOK;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    string inputType = cmbInput.SelectedItem.ToString();
                    var fields = _inputRequiredFields[inputType];
                    bool hasError = false;
                    var inputValues = new Dictionary<string, string>();
                    foreach (var field in fields)
                    {
                        Control ctrl = panelFields.Controls["txt" + field];
                        string value = null;
                        if (ctrl is ComboBox cb)
                            value = cb.Text.Trim();
                        else if (ctrl is TextBox tb)
                            value = tb.Text.Trim();
                        if (string.IsNullOrEmpty(value))
                        {
                            MessageBox.Show($"Trường '{field}' không được để trống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            hasError = true;
                            break;
                        }
                        inputValues[field] = value;
                    }
                    if (!hasError)
                    {
                        _lastAddedInputType = inputType;
                        _lastAddedInputValues = inputValues;
                        // Xóa hết config cũ, chỉ giữ input mới
                        _configValues.Clear();
                        foreach (var kv in inputValues)
                        {
                            _configValues[kv.Key] = kv.Value;
                        }
                        CreateDynamicControls();
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string logType = GetSelectedLogType();
                _fluentBitConfig.UpdateInputConfig(logType, _configValues);
                string lastInput = _lastAddedInputType;
                if (!string.IsNullOrEmpty(_lastAddedInputType) && _lastAddedInputValues != null)
                {
                    WriteNewInputBlockToFluentBitConf(_lastAddedInputType, _lastAddedInputValues);
                    _lastAddedInputType = null;
                    _lastAddedInputValues = null;
                }
                // Nếu có trường Parser và là parser mới thì thêm vào file parsers.conf
                if (_configValues.ContainsKey("Parser"))
                {
                    string parserName = _configValues["Parser"];
                    var parsers = GetParsersFromFile();
                    if (!string.IsNullOrEmpty(parserName) && !parsers.Contains(parserName))
                    {
                        AddParserToFile(parserName);
                    }
                }
                MessageBox.Show("Cấu hình đã được lưu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                cmbLogType.SelectedIndexChanged -= CmbLogType_SelectedIndexChanged;
                LoadConfig();
                if (!string.IsNullOrEmpty(lastInput) && cmbLogType.Items.Contains(lastInput))
                {
                    cmbLogType.SelectedItem = lastInput;
                }
                cmbLogType.SelectedIndexChanged += CmbLogType_SelectedIndexChanged;
                if (!System.IO.Directory.Exists("logs"))
                {
                    System.IO.Directory.CreateDirectory("logs");
                }
                FluentBitHelper.RestartFluentBitWithNotify();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WriteNewInputBlockToFluentBitConf(string inputType, Dictionary<string, string> inputValues)
        {
            string filePath = "fluent-bit.conf";
            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show($"Không tìm thấy file cấu hình: {filePath}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var lines = System.IO.File.ReadAllLines(filePath);
            int lastInputIdx = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[INPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    lastInputIdx = i;
                }
            }
            int insertIdx = lines.Length;
            if (lastInputIdx != -1)
            {
                insertIdx = lastInputIdx + 1;
                while (insertIdx < lines.Length && (string.IsNullOrWhiteSpace(lines[insertIdx]) || !lines[insertIdx].Trim().StartsWith("[")))
                {
                    insertIdx++;
                }
            }
            var newBlock = new List<string> { "[INPUT]" };
            newBlock.Add($"    Name              {inputType}");
            foreach (var kv in inputValues)
            {
                newBlock.Add($"    {kv.Key.PadRight(18)}{kv.Value}");
            }
            newBlock.Add("");
            var newLines = new List<string>(lines);
            newLines.InsertRange(insertIdx, newBlock);
            System.IO.File.WriteAllLines(filePath, newLines);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _configValues.Clear();
            foreach (var item in _originalConfigValues)
            {
                _configValues[item.Key] = item.Value;
            }
            CreateDynamicControls();
        }

        private string GetSelectedLogType()
        {
            return cmbLogType.SelectedItem?.ToString() ?? string.Empty;
        }

        private void CreateDynamicControls()
        {
            panelConfig.Controls.Clear();
            panelConfig.Controls.Add(cmbLogType);
            panelConfig.Controls.Add(btnSave);
            panelConfig.Controls.Add(btnReset);
            panelConfig.Controls.Add(btnAddConfig);

            int y = 60;
            string logType = GetSelectedLogType();
            foreach (var kv in _configValues)
            {
                var label = new Guna2HtmlLabel
                {
                    Text = kv.Key,
                    Location = new Point(10, y),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.White
                };
                panelConfig.Controls.Add(label);

                // Nếu là trường Parser và logType là syslog, dùng ComboBox
                if (kv.Key.Equals("Parser", StringComparison.OrdinalIgnoreCase))
                {
                    var parserCombo = new ComboBox
                    {
                        Name = kv.Key,
                        Location = new Point(220, y),
                        Size = new Size(300, 36),
                        Font = new Font("Segoe UI", 10),
                        DropDownStyle = ComboBoxStyle.DropDown,
                        BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
                        ForeColor = System.Drawing.Color.White
                    };
                    var parsers = GetParsersFromFile();
                    parserCombo.Items.AddRange(parsers.ToArray());
                    parserCombo.Text = kv.Value;
                    parserCombo.Leave += (s, e) => SaveSingleConfig(kv.Key, parserCombo.Text);
                    panelConfig.Controls.Add(parserCombo);
                }
                else
                {
                    var textBox = new Guna2TextBox
                    {
                        Name = kv.Key,
                        Text = kv.Value,
                        Location = new Point(220, y),
                        Size = new Size(300, 36),
                        Font = new Font("Segoe UI", 10),
                        FillColor = Color.FromArgb(40, 40, 40),
                        ForeColor = Color.White,
                        BorderRadius = 8
                    };
                    textBox.TextChanged += (s, e) => SaveSingleConfig(kv.Key, textBox.Text);
                    panelConfig.Controls.Add(textBox);
                }
                y += 50;
            }
        }

        private void SaveSingleConfig(string key, string value)
        {
            if (_configValues.ContainsKey(key))
            {
                _configValues[key] = value;
            }
        }

        private void LoadConfig()
        {
            try
            {
                var inputNames = _fluentBitConfig.GetAllInputNames();
                string currentSelected = cmbLogType.SelectedItem?.ToString();

                cmbLogType.SelectedIndexChanged -= CmbLogType_SelectedIndexChanged;
                cmbLogType.Items.Clear();
                if (inputNames != null)
                {
                    foreach (var name in inputNames)
                    {
                        cmbLogType.Items.Add(name);
                    }
                }
                // Giữ lại selection nếu có
                if (!string.IsNullOrEmpty(currentSelected) && cmbLogType.Items.Contains(currentSelected))
                    cmbLogType.SelectedItem = currentSelected;
                else if (cmbLogType.Items.Count > 0 && cmbLogType.SelectedIndex < 0)
                    cmbLogType.SelectedIndex = 0;
                cmbLogType.SelectedIndexChanged += CmbLogType_SelectedIndexChanged;

                string logType = GetSelectedLogType();
                _configValues.Clear();
                foreach (var item in _fluentBitConfig.GetInputConfig(logType))
                {
                    _configValues[item.Key] = item.Value;
                }
                _originalConfigValues.Clear();
                foreach (var item in _configValues)
                {
                    _originalConfigValues[item.Key] = item.Value;
                }

                CreateDynamicControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Đọc danh sách parser từ parsers.conf
        private List<string> GetParsersFromFile()
        {
            var parsers = new List<string>();
            if (!System.IO.File.Exists("parsers.conf")) return parsers;
            var lines = System.IO.File.ReadAllLines("parsers.conf");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[PARSER]", StringComparison.OrdinalIgnoreCase))
                {
                    int j = i + 1;
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        var line = lines[j].Trim();
                        if (line.StartsWith("Name"))
                        {
                            var name = line.Substring(4).Trim();
                            if (!string.IsNullOrEmpty(name))
                                parsers.Add(name);
                            break;
                        }
                        j++;
                    }
                }
            }
            return parsers;
        }

        // Thêm parser mới vào file parsers.conf
        private void AddParserToFile(string parserName)
        {
            var block = new List<string>
            {
                "[PARSER]",
                $"    Name        {parserName}",
                "    Format      regex",
                "    Regex       .*",
                "    Time_Key    timestamp",
                "    Time_Format %Y-%m-%dT%H:%M:%S.%L%z",
                "    Time_Keep   On",
                ""
            };
            System.IO.File.AppendAllLines("parsers.conf", block);
        }
    }
}