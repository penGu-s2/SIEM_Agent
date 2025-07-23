namespace SIEM_Agent.UI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Guna.UI2.WinForms.Guna2Panel sidebarPanel;
        private Guna.UI2.WinForms.Guna2Panel topbarPanel;
        private Guna.UI2.WinForms.Guna2Panel mainContentPanel;
        private Guna.UI2.WinForms.Guna2Button btnDashboard;
        private Guna.UI2.WinForms.Guna2Button btnLogsManagement;
        private Guna.UI2.WinForms.Guna2Button btnSystemSettings;
        private Guna.UI2.WinForms.Guna2Button btnParserSettings;
        private Guna.UI2.WinForms.Guna2Button btnExit;
        private Guna.UI2.WinForms.Guna2CirclePictureBox avatarPictureBox;
        private Guna.UI2.WinForms.Guna2TextBox searchTextBox;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.FlowLayoutPanel sidebarFlowPanel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.TableLayoutPanel topBarLayout;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.Panel footerPanel;
        private System.Windows.Forms.Label lblFooterInfo;
        private System.Windows.Forms.Label lblClock;
        private System.Windows.Forms.Timer timerClock;
        private System.Windows.Forms.Label lblFluentBitStatus;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.sidebarPanel = new Guna.UI2.WinForms.Guna2Panel();
            this.topbarPanel = new Guna.UI2.WinForms.Guna2Panel();
            this.mainContentPanel = new Guna.UI2.WinForms.Guna2Panel();
            this.btnDashboard = new Guna.UI2.WinForms.Guna2Button();
            this.btnLogsManagement = new Guna.UI2.WinForms.Guna2Button();
            this.btnSystemSettings = new Guna.UI2.WinForms.Guna2Button();
            this.btnParserSettings = new Guna.UI2.WinForms.Guna2Button();
            this.btnExit = new Guna.UI2.WinForms.Guna2Button();
            this.avatarPictureBox = new Guna.UI2.WinForms.Guna2CirclePictureBox();
            this.searchTextBox = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblUserName = new System.Windows.Forms.Label();
            this.sidebarFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.lblAppName = new System.Windows.Forms.Label();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topBarLayout = new System.Windows.Forms.TableLayoutPanel();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.footerPanel = new System.Windows.Forms.Panel();
            this.lblFooterInfo = new System.Windows.Forms.Label();
            this.lblClock = new System.Windows.Forms.Label();
            this.timerClock = new System.Windows.Forms.Timer(this.components);
            this.lblFluentBitStatus = new System.Windows.Forms.Label();

            // MainForm
            this.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SIEM Agent";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Main Layout
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.RowCount = 3;
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60)); // TopBar
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); // Content
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30)); // Footer
            this.mainLayout.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);

            // TopBar Layout
            this.topBarLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topBarLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topBarLayout.ColumnCount = 2;
            this.topBarLayout.RowCount = 1;
            this.topBarLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220)); // Logo + AppName
            this.topBarLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // User
            this.topBarLayout.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);

            // Content Layout
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.RowCount = 1;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220)); // Sidebar
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // MainContent
            this.contentLayout.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);

            // Footer Panel
            this.footerPanel = new System.Windows.Forms.Panel();
            this.footerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerPanel.BackColor = System.Drawing.Color.FromArgb(24, 24, 24);
            this.lblFooterInfo = new System.Windows.Forms.Label();
            this.lblFooterInfo.Text = "Version 1.0.0 | © 2025";
            this.lblFooterInfo.ForeColor = System.Drawing.Color.White;
            this.lblFooterInfo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFooterInfo.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblFooterInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblFooterInfo.Width = 200;
            // Clock label
            this.lblClock.Text = "--:--:--";
            this.lblClock.ForeColor = System.Drawing.Color.White;
            this.lblClock.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblClock.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblClock.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblClock.Width = 120;
            this.footerPanel.Controls.Add(this.lblFooterInfo);
            this.footerPanel.Controls.Add(this.lblClock);
            this.lblFluentBitStatus = new System.Windows.Forms.Label();
            this.lblFluentBitStatus.AutoSize = true;
            this.lblFluentBitStatus.ForeColor = System.Drawing.Color.LightGreen;
            this.lblFluentBitStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFluentBitStatus.Text = "Fluent Bit: Đang kiểm tra...";
            this.lblFluentBitStatus.Location = new System.Drawing.Point(300, 5);
            this.footerPanel.Controls.Add(this.lblFluentBitStatus);
            // Timer cho clock
            this.timerClock.Interval = 1000;
            this.timerClock.Tick += (s, e) => { this.lblClock.Text = DateTime.Now.ToString("HH:mm:ss"); };
            this.timerClock.Start();

            // Logo + AppName (TopBar Left)
            this.logoPictureBox.Size = new System.Drawing.Size(32, 32);
            this.logoPictureBox.Margin = new System.Windows.Forms.Padding(10, 14, 0, 14);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.BackColor = System.Drawing.Color.Transparent;
            this.logoPictureBox.Image = Image.FromStream(new MemoryStream(SIEM_Agent.UI.Forms.Properties.Resources.dashboard)); // Đảm bảo lấy ảnh từ resources
            this.lblAppName.Text = "SIEM Solution";
            this.lblAppName.ForeColor = System.Drawing.Color.White;
            this.lblAppName.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblAppName.AutoSize = true;
            this.lblAppName.Margin = new System.Windows.Forms.Padding(10, 18, 0, 0);
            var logoAppPanel = new System.Windows.Forms.Panel();
            logoAppPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            logoAppPanel.BackColor = System.Drawing.Color.Transparent;
            logoAppPanel.Controls.Add(this.logoPictureBox);
            logoAppPanel.Controls.Add(this.lblAppName);
            this.logoPictureBox.Location = new System.Drawing.Point(10, 14);
            this.lblAppName.Location = new System.Drawing.Point(50, 18);

            // User (TopBar Right)
            var userPanel = new System.Windows.Forms.Panel();
            userPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            userPanel.BackColor = System.Drawing.Color.Transparent;
            this.avatarPictureBox.Size = new System.Drawing.Size(40, 40);
            this.avatarPictureBox.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.avatarPictureBox.Location = new System.Drawing.Point(160, 10);
            this.lblUserName.Text = "User";
            this.lblUserName.ForeColor = System.Drawing.Color.White;
            this.lblUserName.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblUserName.AutoSize = true;
            this.lblUserName.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.lblUserName.Location = new System.Drawing.Point(120, 20);
            this.lblUserName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            userPanel.Controls.Add(this.avatarPictureBox);
            userPanel.Controls.Add(this.lblUserName);

            // Thêm vào topBarLayout (logo+appname bên trái, user/avatar bên phải)
            this.topBarLayout.Controls.Add(logoAppPanel, 0, 0);
            this.topBarLayout.Controls.Add(userPanel, 1, 0);

            // Sidebar (Content Left)
            this.sidebarFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sidebarFlowPanel.Width = 220;
            this.sidebarFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.sidebarFlowPanel.WrapContents = false;
            this.sidebarFlowPanel.AutoScroll = true;
            this.sidebarFlowPanel.BackColor = System.Drawing.Color.FromArgb(24, 24, 24);
            this.sidebarFlowPanel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            this.sidebarFlowPanel.Controls.Clear();
            this.sidebarFlowPanel.Controls.Add(this.btnDashboard);
            this.sidebarFlowPanel.Controls.Add(this.btnLogsManagement);
            this.sidebarFlowPanel.Controls.Add(this.btnSystemSettings);
            this.sidebarFlowPanel.Controls.Add(this.btnParserSettings);
            this.sidebarFlowPanel.Controls.Add(this.btnExit);
            foreach (var btn in new[] { btnDashboard, btnLogsManagement, btnSystemSettings, btnParserSettings, btnExit })
            {
                btn.AutoSize = false;
                btn.Width = 200;
                btn.Height = 45;
                btn.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
                btn.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left;
                btn.TextOffset = new System.Drawing.Point(16, 0);
                btn.ImageOffset = new System.Drawing.Point(8, 0);
                btn.Font = new System.Drawing.Font("Segoe UI", 10F);
                btn.FillColor = System.Drawing.Color.FromArgb(32, 32, 32);
                btn.ForeColor = System.Drawing.Color.White;
                btn.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            }

            // MainContentPanel (Content Right)
            this.mainContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainContentPanel.FillColor = System.Drawing.Color.FromArgb(32, 32, 32);
            this.mainContentPanel.BorderRadius = 12;

            // Thêm vào contentLayout
            this.contentLayout.Controls.Add(this.sidebarFlowPanel, 0, 0);
            this.contentLayout.Controls.Add(this.mainContentPanel, 1, 0);

            // Thêm các panel vào mainLayout
            this.mainLayout.Controls.Add(this.topBarLayout, 0, 0);
            this.mainLayout.Controls.Add(this.contentLayout, 0, 1);
            this.mainLayout.Controls.Add(this.footerPanel, 0, 2);

            // Thêm mainLayout vào MainForm
            this.Controls.Clear();
            this.Controls.Add(this.mainLayout);
        }

        #endregion
    }
}