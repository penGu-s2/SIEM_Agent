using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System;
using ScottPlot;
using ScottPlot.WinForms;
using System.Collections.Generic;
using System.Linq;
using SIEM_Agent.Core.Config;
using SIEM_Agent.Core;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class DashboardControl : System.Windows.Forms.UserControl
    {
        private System.Windows.Forms.Label lblCpu, lblRam, lblDisk, lblNetIn, lblNetOut;
        private System.Windows.Forms.Timer timer;
        private PerformanceCounter cpuCounter, ramCounter, diskCounter, netInCounter, netOutCounter;
        private ScottPlot.WinForms.FormsPlot formsPlot;
        private List<double> cpuHistory = new List<double>();
        private List<double> ramHistory = new List<double>();
        private List<double> diskHistory = new List<double>();
        private const int MaxPoints = 60;

        public DashboardControl()
        {
            this.BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
            this.Dock = DockStyle.Fill;

            // Panel thông tin phần mềm (giữ nguyên ở trên)
            var infoPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = System.Drawing.Color.FromArgb(28, 28, 28),
                Padding = new System.Windows.Forms.Padding(20, 10, 10, 10)
            };
            var lblAppName = new System.Windows.Forms.Label
            {
                Text = "SIEM Agent - Hệ thống quản lý log tập trung",
                Font = new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Location = new System.Drawing.Point(0, 0)
            };
            var lblVersion = new System.Windows.Forms.Label
            {
                Text = $"Phiên bản: {System.Windows.Forms.Application.ProductVersion}",
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Location = new System.Drawing.Point(0, 40)
            };
            var lblAuthor = new System.Windows.Forms.Label
            {
                Text = "Tác giả: DreamTeam",
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Location = new System.Drawing.Point(300, 40)
            };
            var lblBuild = new System.Windows.Forms.Label
            {
                Text = $"Ngày build: {DateTime.Now:yyyy-MM-dd}",
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Location = new System.Drawing.Point(600, 40)
            };
            infoPanel.Controls.Add(lblAppName);
            infoPanel.Controls.Add(lblVersion);
            infoPanel.Controls.Add(lblAuthor);
            infoPanel.Controls.Add(lblBuild);

            // Panel chứa TabControl ở dưới
            var panelTabs = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32)
            };

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Regular)
            };

            // Tab 1: Output HTTP
            var tabHttp = new TabPage("Output HTTP")
            {
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32)
            };
            tabHttp.Controls.Add(new HttpOutputControl());

            // Tab 2: Output Opensearch
            var tabOpensearch = new TabPage("Output Opensearch")
            {
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32)
            };
            tabOpensearch.Controls.Add(new OpensearchOutputControl());

            // Tab 3: Output File
            var tabFile = new TabPage("Output File")
            {
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32)
            };
            // Thêm trực tiếp phần cấu hình output file vào tab này
            var panelFile = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32)
            };
            // Tiêu đề
            var lblOutputTitle = new System.Windows.Forms.Label
            {
                Text = "Bật/Tắt Output lấy log cho từng nguồn dữ liệu bên dưới. Chỉ những nguồn được bật mới được ghi log ra file.",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.LightSkyBlue,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new System.Windows.Forms.Padding(10, 10, 10, 10)
            };
            // CheckedListBox
            var fluentBitConfig = new FluentBitConfig("fluent-bit.conf");
            var inputNames = fluentBitConfig.GetAllInputNames();
            var checkedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Regular),
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32),
                ForeColor = System.Drawing.Color.White,
                CheckOnClick = true
            };
            var enabledTags = GetEnabledOutputTags();
            foreach (var input in inputNames)
            {
                var config = fluentBitConfig.GetInputConfig(input);
                string tag = config.ContainsKey("Tag") ? config["Tag"] : "";
                string display = $"{input} (Tag: {tag})";
                bool isChecked = !string.IsNullOrEmpty(tag) && enabledTags.Contains(tag);
                checkedListBox.Items.Add(display, isChecked);
            }
            checkedListBox.ItemCheck += (s, e) =>
            {
                panelFile.BeginInvoke(new Action(() =>
                {
                    UpdateOutputBlocks(inputNames, checkedListBox, fluentBitConfig);
                }));
            };
            panelFile.Controls.Add(checkedListBox);
            panelFile.Controls.Add(lblOutputTitle);
            tabFile.Controls.Add(panelFile);

            tabControl.TabPages.Add(tabHttp);
            tabControl.TabPages.Add(tabOpensearch);
            tabControl.TabPages.Add(tabFile);

            panelTabs.Controls.Add(tabControl);

            this.Controls.Add(panelTabs);
            this.Controls.Add(infoPanel);
        }

        private HashSet<string> GetEnabledOutputTags()
        {
            var tags = new HashSet<string>();
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    string tag = null;
                    int j = i + 1;
                    while (j < lines.Length && !lines[j].Trim().StartsWith("["))
                    {
                        var line = lines[j].Trim();
                        if (line.StartsWith("Match"))
                        {
                            tag = line.Substring(5).Trim();
                            break;
                        }
                        j++;
                    }
                    if (!string.IsNullOrEmpty(tag))
                        tags.Add(tag);
                }
            }
            return tags;
        }

        private void UpdateOutputBlocks(IEnumerable<string> inputNames, CheckedListBox checkedListBox, FluentBitConfig fluentBitConfig)
        {
            var lines = System.IO.File.ReadAllLines("fluent-bit.conf").ToList();
            // Xóa hết các block [OUTPUT] ghi ra file
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Trim().Equals("[OUTPUT]", StringComparison.OrdinalIgnoreCase))
                {
                    int start = i;
                    int end = i + 1;
                    while (end < lines.Count && !lines[end].Trim().StartsWith("[")) end++;
                    // Kiểm tra nếu là output file
                    bool isFileOutput = false;
                    for (int j = start + 1; j < end; j++)
                    {
                        if (lines[j].Trim().StartsWith("Name") && lines[j].ToLower().Contains("file"))
                        {
                            isFileOutput = true;
                            break;
                        }
                    }
                    if (isFileOutput)
                        lines.RemoveRange(start, end - start);
                }
            }
            // Thêm lại các block [OUTPUT] cho các input được bật
            for (int idx = 0; idx < checkedListBox.Items.Count; idx++)
            {
                if (checkedListBox.GetItemChecked(idx))
                {
                    string display = checkedListBox.Items[idx].ToString();
                    string input = display.Split(' ')[0];
                    var config = fluentBitConfig.GetInputConfig(input);
                    string tag = config.ContainsKey("Tag") ? config["Tag"] : input;
                    string fileName = tag + ".log";
                    var block = new List<string>
                    {
                        "[OUTPUT]",
                        "    Name file",
                        $"    Match {tag}",
                        "    Path .\\logs\\",
                        $"    File {fileName}",
                        "    Format plain",
                        "    Retry_Limit 3",
                        ""
                    };
                    lines.AddRange(block);
                }
            }
            System.IO.File.WriteAllLines("fluent-bit.conf", lines);
            FluentBitHelper.RestartFluentBitWithNotify();
        }
    }
} 