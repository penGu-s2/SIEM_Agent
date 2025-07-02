using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class OpensearchOutputControl : UserControl
    {
        private ListView listViewOutputs;
        private Button btnAdd;
        private List<string> inputNames;
        private CheckedListBox checkedListBox;
        private Dictionary<string, Dictionary<string, string>> configValues = new Dictionary<string, Dictionary<string, string>>();
        private bool _suppressPopup = false;
        private Button btnDelete;
        public OpensearchOutputControl()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(32, 32, 32);

            // Lấy danh sách inputNames từ FluentBitConfig
            var fluentBitConfig = new SIEM_Agent.Core.Config.FluentBitConfig("fluent-bit.conf");
            inputNames = fluentBitConfig.GetAllInputNames();
            LoadOpensearchConfigsFromFile();

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(32, 32, 32),
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            // Bên trái: checkedListBox
            var lblTitle = new Label
            {
                Text = "Cấu hình gửi log qua Opensearch cho từng nguồn dữ liệu",
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
                        using (var popup = new OpensearchConfigPopupForm())
                        {
                            if (oldCfg != null)
                            {
                                // Gán lại giá trị cũ nếu có
                                var txtMatch = popup.Controls.Find("txtMatch", true).OfType<TextBox>().FirstOrDefault();
                                if (txtMatch != null) txtMatch.Text = oldCfg.GetValueOrDefault("Match", input);
                                var txtHost = popup.Controls.Find("txtHost", true).OfType<TextBox>().FirstOrDefault();
                                if (txtHost != null) txtHost.Text = oldCfg.GetValueOrDefault("Host", "localhost");
                                var txtPort = popup.Controls.Find("txtPort", true).OfType<TextBox>().FirstOrDefault();
                                if (txtPort != null) txtPort.Text = oldCfg.GetValueOrDefault("Port", "9200");
                                var txtUser = popup.Controls.Find("txtUser", true).OfType<TextBox>().FirstOrDefault();
                                if (txtUser != null) txtUser.Text = oldCfg.GetValueOrDefault("HTTP_User", "");
                                var txtPass = popup.Controls.Find("txtPass", true).OfType<TextBox>().FirstOrDefault();
                                if (txtPass != null) txtPass.Text = oldCfg.GetValueOrDefault("HTTP_Passwd", "");
                                var txtIndex = popup.Controls.Find("txtIndex", true).OfType<TextBox>().FirstOrDefault();
                                if (txtIndex != null) txtIndex.Text = oldCfg.GetValueOrDefault("Index", "fluentbit-logs");
                                var txtType = popup.Controls.Find("txtType", true).OfType<TextBox>().FirstOrDefault();
                                if (txtType != null) txtType.Text = oldCfg.GetValueOrDefault("Type", "_doc");
                                var txtTls = popup.Controls.Find("txtTls", true).OfType<TextBox>().FirstOrDefault();
                                if (txtTls != null) txtTls.Text = oldCfg.GetValueOrDefault("tls", "On");
                                var txtTlsVerify = popup.Controls.Find("txtTlsVerify", true).OfType<TextBox>().FirstOrDefault();
                                if (txtTlsVerify != null) txtTlsVerify.Text = oldCfg.GetValueOrDefault("tls.verify", "Off");
                                var txtSuppressType = popup.Controls.Find("txtSuppressType", true).OfType<TextBox>().FirstOrDefault();
                                if (txtSuppressType != null) txtSuppressType.Text = oldCfg.GetValueOrDefault("Suppress_Type_Name", "On");
                                var txtLogstash = popup.Controls.Find("txtLogstash", true).OfType<TextBox>().FirstOrDefault();
                                if (txtLogstash != null) txtLogstash.Text = oldCfg.GetValueOrDefault("Logstash_Format", "On");
                            }
                            var result = popup.ShowDialog();
                            if (result == DialogResult.OK && popup.ResultConfig != null)
                            {
                                configValues[input] = popup.ResultConfig;
                                SaveOpensearchOutputs();
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
                        SaveOpensearchOutputs();
                    }));
                }
            };
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(32, 32, 32)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftLayout.Controls.Add(lblTitle, 0, 0);
            leftLayout.Controls.Add(checkedListBox, 0, 1);
            mainLayout.Controls.Add(leftLayout, 0, 0);

            // Bên phải: ListView + nút xóa
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 32, 32) };
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
            listViewOutputs.Columns.Add("Match", 80);
            listViewOutputs.Columns.Add("Host", 100);
            listViewOutputs.Columns.Add("Port", 60);
            listViewOutputs.Columns.Add("HTTP_User", 100);
            listViewOutputs.Columns.Add("HTTP_Passwd", 100);
            listViewOutputs.Columns.Add("Index", 120);
            listViewOutputs.Columns.Add("Type", 80);
            listViewOutputs.Columns.Add("tls", 60);
            listViewOutputs.Columns.Add("tls.verify", 80);
            listViewOutputs.Columns.Add("Suppress_Type_Name", 120);
            listViewOutputs.Columns.Add("Logstash_Format", 120);
            rightPanel.Controls.Add(listViewOutputs);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainLayout);
            LoadOpensearchOutputsToListView();
        }

        private void LoadOpensearchConfigsFromFile()
        {
            configValues.Clear();
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf");
            Dictionary<string, string> block = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (line.Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    if (block.Count > 0 && block.ContainsKey("Name") && block["Name"].Trim().Equals("opensearch", StringComparison.OrdinalIgnoreCase))
                    {
                        string input = block.ContainsKey("Match") ? block["Match"].Trim() : "";
                        if (!string.IsNullOrEmpty(input))
                            configValues[input] = new Dictionary<string, string>(block);
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
            if (block.Count > 0 && block.ContainsKey("Name") && block["Name"].Trim().Equals("opensearch", StringComparison.OrdinalIgnoreCase))
            {
                string input = block.ContainsKey("Match") ? block["Match"].Trim() : "";
                if (!string.IsNullOrEmpty(input))
                    configValues[input] = new Dictionary<string, string>(block);
            }
        }

        private void SaveOpensearchOutputs()
        {
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf").ToList();
            // Xóa các block [OUTPUT] opensearch hiện có
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int start = i;
                    int end = i + 1;
                    while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                    bool isOpensearch = false;
                    string match = null;
                    for (int j = start + 1; j < end; j++)
                    {
                        if (lines[j].Trim().StartsWith("Name") && lines[j].ToLower().Contains("opensearch"))
                            isOpensearch = true;
                        if (lines[j].Trim().StartsWith("Match"))
                            match = lines[j].Trim().Substring(5).Trim();
                    }
                    if (isOpensearch && match != null)
                        lines.RemoveRange(start, end - start);
                }
            }
            // Thêm lại các block [OUTPUT] opensearch cho từng nguồn được bật
            foreach (var input in inputNames)
            {
                if (configValues.ContainsKey(input))
                {
                    var cfg = configValues[input];
                    var block = new List<string>
                    {
                        "[OUTPUT]",
                        "    Name  opensearch",
                        $"    Match {input}",
                        $"    Host  {(cfg.ContainsKey("Host") ? cfg["Host"] : "localhost")}",
                        $"    Port  {(cfg.ContainsKey("Port") ? cfg["Port"] : "9200")}",
                        $"    HTTP_User {(cfg.ContainsKey("HTTP_User") ? cfg["HTTP_User"] : "")}",
                        $"    HTTP_Passwd {(cfg.ContainsKey("HTTP_Passwd") ? cfg["HTTP_Passwd"] : "")}",
                        $"    Index {(cfg.ContainsKey("Index") ? cfg["Index"] : "fluentbit-logs")}",
                        $"    Type {(cfg.ContainsKey("Type") ? cfg["Type"] : "_doc")}",
                        $"    tls {(cfg.ContainsKey("tls") ? cfg["tls"] : "On")}",
                        $"    tls.verify {(cfg.ContainsKey("tls.verify") ? cfg["tls.verify"] : "Off")}",
                        $"    Suppress_Type_Name {(cfg.ContainsKey("Suppress_Type_Name") ? cfg["Suppress_Type_Name"] : "On")}",
                        $"    Logstash_Format {(cfg.ContainsKey("Logstash_Format") ? cfg["Logstash_Format"] : "On")}",
                        ""
                    };
                    lines.AddRange(block);
                }
            }
            System.IO.File.WriteAllLines("fluent-bit.conf", lines);
            LoadOpensearchConfigsFromFile();
            // Cập nhật lại checkedListBox
            _suppressPopup = true;
            checkedListBox.Items.Clear();
            foreach (var input in inputNames)
            {
                checkedListBox.Items.Add(input, configValues.ContainsKey(input));
            }
            _suppressPopup = false;
            LoadOpensearchOutputsToListView();
            SIEM_Agent.Core.FluentBitHelper.RestartFluentBitWithNotify();
        }

        private void LoadOpensearchOutputsToListView()
        {
            listViewOutputs.Items.Clear();
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf");
            Dictionary<string, string> block = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (line.Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    if (block.Count > 0 && block.ContainsKey("Name") && block["Name"].Trim().Equals("opensearch", StringComparison.OrdinalIgnoreCase))
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
            if (block.Count > 0 && block.ContainsKey("Name") && block["Name"].Trim().Equals("opensearch", StringComparison.OrdinalIgnoreCase))
            {
                AddBlockToListView(block);
            }
        }

        private void AddBlockToListView(Dictionary<string, string> block)
        {
            var lvi = new ListViewItem(new string[]
            {
                block.ContainsKey("Match") ? block["Match"].Trim() : "",
                block.ContainsKey("Host") ? block["Host"].Trim() : "",
                block.ContainsKey("Port") ? block["Port"].Trim() : "",
                block.ContainsKey("HTTP_User") ? block["HTTP_User"].Trim() : "",
                block.ContainsKey("HTTP_Passwd") ? block["HTTP_Passwd"].Trim() : "",
                block.ContainsKey("Index") ? block["Index"].Trim() : "",
                block.ContainsKey("Type") ? block["Type"].Trim() : "",
                block.ContainsKey("tls") ? block["tls"].Trim() : "",
                block.ContainsKey("tls.verify") ? block["tls.verify"].Trim() : "",
                block.ContainsKey("Suppress_Type_Name") ? block["Suppress_Type_Name"].Trim() : "",
                block.ContainsKey("Logstash_Format") ? block["Logstash_Format"].Trim() : ""
            });
            listViewOutputs.Items.Add(lvi);
        }

        private void DeleteSelectedOutput()
        {
            if (listViewOutputs.SelectedItems.Count == 0) return;
            var input = listViewOutputs.SelectedItems[0].SubItems[0].Text;
            // Xóa block [OUTPUT] opensearch có Match = input
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf").ToList();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int start = i;
                    int end = i + 1;
                    while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                    bool isOpensearch = false;
                    string match = null;
                    for (int j = start + 1; j < end; j++)
                    {
                        if (lines[j].Trim().StartsWith("Name") && lines[j].ToLower().Contains("opensearch"))
                            isOpensearch = true;
                        if (lines[j].Trim().StartsWith("Match"))
                            match = lines[j].Trim().Substring(5).Trim();
                    }
                    if (isOpensearch && match == input)
                        lines.RemoveRange(start, end - start);
                }
            }
            System.IO.File.WriteAllLines("fluent-bit.conf", lines);
            LoadOpensearchConfigsFromFile();
            _suppressPopup = true;
            checkedListBox.Items.Clear();
            foreach (var name in inputNames)
            {
                checkedListBox.Items.Add(name, configValues.ContainsKey(name));
            }
            _suppressPopup = false;
            LoadOpensearchOutputsToListView();
            SIEM_Agent.Core.FluentBitHelper.RestartFluentBitWithNotify();
        }
    }
} 