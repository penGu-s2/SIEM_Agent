using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class AlertsControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2DataGridView gridAlerts;
        private readonly Guna2Button btnNewAlert;
        private readonly Guna2Button btnExport;
        private readonly Guna2Button btnDelete;
        private readonly Guna2ComboBox cmbSeverity;
        private readonly Guna2ComboBox cmbStatus;

        public AlertsControl()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.Dock = DockStyle.Fill;

            // Panel chính
            panelMain = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.Transparent,
                BorderRadius = 0,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            // Grid hiển thị danh sách cảnh báo
            gridAlerts = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                },
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // Thêm các cột (chỉ set Width cho cột nhỏ nếu cần)
            gridAlerts.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Tiêu đề" },
                new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Mức độ" },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái" },
                new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Thời gian" },
                new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Nguồn" }
            });

            // Panel chứa các nút và bộ lọc
            var panelTop = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FillColor = Color.Transparent,
                BorderRadius = 0,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            // ComboBox lọc theo mức độ
            cmbSeverity = new Guna2ComboBox
            {
                Size = new Size(120, 36),
                Location = new Point(10, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbSeverity.Items.AddRange(new string[] { "Tất cả", "Cao", "Trung bình", "Thấp" });
            cmbSeverity.SelectedIndex = 0;
            cmbSeverity.SelectedIndexChanged += CmbSeverity_SelectedIndexChanged;

            // ComboBox lọc theo trạng thái
            cmbStatus = new Guna2ComboBox
            {
                Size = new Size(120, 36),
                Location = new Point(140, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbStatus.Items.AddRange(new string[] { "Tất cả", "Mới", "Đang xử lý", "Đã xử lý" });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.SelectedIndexChanged += CmbStatus_SelectedIndexChanged;

            // Nút tạo cảnh báo mới
            btnNewAlert = new Guna2Button
            {
                Text = "Tạo cảnh báo mới",
                Size = new Size(150, 36),
                Location = new Point(270, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnNewAlert.Click += BtnNewAlert_Click;

            // Nút xuất báo cáo
            btnExport = new Guna2Button
            {
                Text = "Xuất báo cáo",
                Size = new Size(150, 36),
                Location = new Point(430, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnExport.Click += BtnExport_Click;

            // Nút xóa
            btnDelete = new Guna2Button
            {
                Text = "Xóa",
                Size = new Size(100, 36),
                Location = new Point(590, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnDelete.Click += BtnDelete_Click;

            // Thêm các control vào panel
            panelTop.Controls.Add(cmbSeverity);
            panelTop.Controls.Add(cmbStatus);
            panelTop.Controls.Add(btnNewAlert);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(btnDelete);

            // Thêm panelTop và gridAlerts vào panelMain
            panelMain.Controls.Clear();
            panelMain.Controls.Add(gridAlerts);
            panelMain.Controls.Add(panelTop);

            // Đảm bảo chỉ có panelMain trong this.Controls
            this.Controls.Clear();
            this.Controls.Add(panelMain);

            // Load dữ liệu
            LoadData();
        }

        private void LoadData()
        {
            // TODO: Load dữ liệu từ database
            gridAlerts.Rows.Clear();
            gridAlerts.Rows.Add("1", "Phát hiện đăng nhập bất thường", "Cao", "Mới", "2025-03-20 10:30", "Windows Event");
            gridAlerts.Rows.Add("2", "Truy cập trái phép", "Trung bình", "Đang xử lý", "2025-03-20 09:15", "Syslog");
        }

        private void CmbSeverity_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo mức độ
            LoadData();
        }

        private void CmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo trạng thái
            LoadData();
        }

        private void BtnNewAlert_Click(object sender, EventArgs e)
        {
            // TODO: Mở form tạo cảnh báo mới
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // TODO: Xuất báo cáo
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridAlerts.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa cảnh báo này?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Xóa cảnh báo
                    gridAlerts.Rows.RemoveAt(gridAlerts.SelectedRows[0].Index);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn cảnh báo cần xóa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 