using System;
using System.Windows.Forms;
using SIEM_Agent.Core.Interfaces;
using SIEM_Agent.Core.Services;
using SIEM_Agent.UI.Forms;
using SIEM_Agent.UI.Forms.Controls;
using System.Drawing;
using Guna.UI2.WinForms;
using SIEM_Agent.UI.Forms.Properties;
using System.IO;
using SIEM_Agent.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SIEM_Agent.UI
{
    public partial class MainForm : Form
    {
        private readonly LogManagementService _logManagementService;
        private WebView2 webView2;

        public MainForm()
        {
            InitializeComponent();
            ConfigureControls();
            ConfigureEvents();
            // Hiển thị DashboardControl khi khởi động
            LoadControl(new DashboardControl());
            UpdateFluentBitStatus();
        }

        public MainForm(LogManagementService logManagementService) : this()
        {
            _logManagementService = logManagementService;
        }

        private void ConfigureControls()
        {
            InitializeSidebar();
            // Khởi tạo WebView2 và thêm vào mainContentPanel nhưng ẩn mặc định
            webView2 = new WebView2();
            webView2.Dock = DockStyle.Fill;
            webView2.Visible = false;
            mainContentPanel.Controls.Add(webView2);
        }

        private void InitializeSidebar()
        {
            // Cấu hình các nút trong menu trái
            var menuButtons = new[] { btnDashboard, btnLogsManagement, btnSystemSettings, btnParserSettings, btnExit };
            foreach (var btn in menuButtons)
            {
                btn.AutoSize = false;
                btn.Width = 200;
                btn.Height = 45;
                btn.ImageAlign = HorizontalAlignment.Left;
                btn.TextAlign = HorizontalAlignment.Left;
                btn.ImageSize = new Size(24, 24);
                btn.ImageOffset = new Point(8, 0);
                btn.TextOffset = new Point(16, 0);
                btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btn.ForeColor = Color.White;
                btn.FillColor = Color.FromArgb(58, 58, 58); // #3a3a3a sáng hơn
                btn.BorderRadius = 8;
                btn.Margin = new Padding(0, 4, 0, 4);
                btn.HoverState.FillColor = Color.FromArgb(80, 80, 80); // #505050
                btn.Dock = DockStyle.None; // Xóa Dock
            }

            btnDashboard.Text = "Dashboard";
            btnDashboard.Image = Image.FromStream(new MemoryStream(Resources.monitor_cog));

            btnLogsManagement.Text = "Logs Management";
            btnLogsManagement.Image = Image.FromStream(new MemoryStream(Resources.clock_2));

            btnSystemSettings.Text = "System Settings";
            btnSystemSettings.Image = Image.FromStream(new MemoryStream(Resources.cog));

            btnParserSettings.Text = "Parser Settings";
            btnParserSettings.Image = Image.FromStream(new MemoryStream(Resources.file_spreadsheet));

            btnExit.Text = "Exit";
            btnExit.Image = Image.FromStream(new MemoryStream(Resources.log_out));

            // Đảm bảo sidebarFlowPanel đủ rộng
            sidebarFlowPanel.Width = 220;
        }

        private void ConfigureEvents()
        {
            btnDashboard.Click += (s, e) => LoadControl(new DashboardControl());
            btnLogsManagement.Click += (s, e) => {
                var repo = new SIEM_Agent.Core.Repositories.LogRepository("logs");
                var logService = new SIEM_Agent.Core.Services.LogManagementService(repo);
                LoadControl(new LogsControl(logService));
            };
            btnSystemSettings.Click += (s, e) => LoadControl(new ConfigControl());
            btnParserSettings.Click += (s, e) => LoadControl(new ParserControl());
            btnExit.Click += (s, e) => {
                CloseAllConnectionsAndExit();
            };
            // Thêm sự kiện click cho nút Dashboard để thử hiển thị WebView2
            btnDashboard.DoubleClick += (s, e) => ShowWebView2Demo();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            UpdateFluentBitStatus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseAllConnectionsAndExit();
            UpdateFluentBitStatus();
            base.OnFormClosing(e);
        }

        private void CloseAllConnectionsAndExit()
        {
            try
            {
                SIEM_Agent.Core.FluentBitHelper.StopFluentBit();
                // Đóng các kết nối khác nếu có
            }
            catch { }
            Application.Exit();
        }

        private void LoadControl(UserControl control)
        {
            mainContentPanel.Controls.Clear();
            control.Dock = DockStyle.Fill;
            mainContentPanel.Controls.Add(control);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x200; // CS_NOCLOSE
                return cp;
            }
        }

        private void UpdateFluentBitStatus()
        {
            if (IsFluentBitRunning())
            {
                lblFluentBitStatus.Text = "Fluent Bit: Đang chạy";
                lblFluentBitStatus.ForeColor = System.Drawing.Color.LightGreen;
            }
            else
            {
                lblFluentBitStatus.Text = "Fluent Bit: Đã dừng";
                lblFluentBitStatus.ForeColor = System.Drawing.Color.OrangeRed;
            }
        }

        private bool IsFluentBitRunning()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("fluent-bit");
                return processes != null && processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void ShowWebView2Demo()
        {
            var webForm = new WebViewForm();
            webForm.Show();
        }
    }
}