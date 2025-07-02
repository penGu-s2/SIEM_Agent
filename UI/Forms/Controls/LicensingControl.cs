using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class LicensingControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2Panel panelLicenseInfo;
        private readonly Guna2Panel panelActivation;
        private readonly Guna2Button btnActivate;
        private readonly Guna2Button btnDeactivate;
        private readonly Guna2Button btnRenew;
        private readonly Guna2TextBox txtLicenseKey;
        private readonly Guna2HtmlLabel lblStatus;
        private readonly Guna2HtmlLabel lblExpiryDate;
        private readonly Guna2HtmlLabel lblFeatures;

        public LicensingControl()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.Dock = DockStyle.Fill;

            // Panel chính
            panelMain = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.FromArgb(28, 28, 28),
                BorderRadius = 10,
                Margin = new Padding(10)
            };

            // Panel thông tin license
            panelLicenseInfo = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                FillColor = Color.FromArgb(40, 40, 40),
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Label trạng thái
            lblStatus = new Guna2HtmlLabel
            {
                Text = "Trạng thái: <span style='color: #4CAF50;'>Đã kích hoạt</span>",
                Location = new Point(20, 20),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };

            // Label ngày hết hạn
            lblExpiryDate = new Guna2HtmlLabel
            {
                Text = "Ngày hết hạn: 31/12/2025",
                Location = new Point(20, 60),
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White
            };

            // Label tính năng
            lblFeatures = new Guna2HtmlLabel
            {
                Text = "Tính năng:<br>" +
                      "- Thu thập và phân tích log<br>" +
                      "- Phát hiện và cảnh báo xâm nhập<br>" +
                      "- Quản lý sự cố và điều tra<br>" +
                      "- Báo cáo và tuân thủ",
                Location = new Point(20, 100),
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White
            };

            // Panel kích hoạt
            panelActivation = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                FillColor = Color.FromArgb(40, 40, 40),
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10)
            };

            // TextBox nhập license key
            txtLicenseKey = new Guna2TextBox
            {
                Size = new Size(300, 36),
                Location = new Point(20, 32),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8,
                PlaceholderText = "Nhập license key..."
            };

            // Nút kích hoạt
            btnActivate = new Guna2Button
            {
                Text = "Kích hoạt",
                Size = new Size(120, 36),
                Location = new Point(330, 32),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnActivate.Click += BtnActivate_Click;

            // Nút hủy kích hoạt
            btnDeactivate = new Guna2Button
            {
                Text = "Hủy kích hoạt",
                Size = new Size(120, 36),
                Location = new Point(460, 32),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnDeactivate.Click += BtnDeactivate_Click;

            // Nút gia hạn
            btnRenew = new Guna2Button
            {
                Text = "Gia hạn",
                Size = new Size(120, 36),
                Location = new Point(590, 32),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnRenew.Click += BtnRenew_Click;

            // Thêm các control vào panel
            panelLicenseInfo.Controls.Add(lblStatus);
            panelLicenseInfo.Controls.Add(lblExpiryDate);
            panelLicenseInfo.Controls.Add(lblFeatures);

            panelActivation.Controls.Add(txtLicenseKey);
            panelActivation.Controls.Add(btnActivate);
            panelActivation.Controls.Add(btnDeactivate);
            panelActivation.Controls.Add(btnRenew);

            panelMain.Controls.Add(panelLicenseInfo);
            panelMain.Controls.Add(panelActivation);
            this.Controls.Add(panelMain);

            // Load thông tin license
            LoadLicenseInfo();
        }

        private void LoadLicenseInfo()
        {
            // TODO: Load thông tin license từ database
            lblStatus.Text = "Trạng thái: <span style='color: #4CAF50;'>Đã kích hoạt</span>";
            lblExpiryDate.Text = "Ngày hết hạn: 31/12/2025";
            lblFeatures.Text = "Tính năng:<br>" +
                             "- Thu thập và phân tích log<br>" +
                             "- Phát hiện và cảnh báo xâm nhập<br>" +
                             "- Quản lý sự cố và điều tra<br>" +
                             "- Báo cáo và tuân thủ";
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLicenseKey.Text))
            {
                MessageBox.Show("Vui lòng nhập license key", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // TODO: Kích hoạt license
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDeactivate_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn hủy kích hoạt license?", "Xác nhận", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                // TODO: Hủy kích hoạt license
                MessageBox.Show("Chức năng đang phát triển", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnRenew_Click(object sender, EventArgs e)
        {
            // TODO: Mở form gia hạn license
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
} 