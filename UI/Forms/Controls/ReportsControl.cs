using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class ReportsControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2DataGridView gridReports;
        private readonly Guna2Button btnNewReport;
        private readonly Guna2Button btnExport;
        private readonly Guna2Button btnDelete;
        private readonly Guna2ComboBox cmbReportType;
        private readonly Guna2DateTimePicker dtpFrom;
        private readonly Guna2DateTimePicker dtpTo;

        public ReportsControl()
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

            // Grid hiển thị danh sách báo cáo
            gridReports = new Guna2DataGridView
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

            // Thêm các cột
            gridReports.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Tiêu đề", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Loại báo cáo", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Ngày tạo", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "Người tạo", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Mô tả", Width = 300 }
            });

            // Panel chứa các nút và bộ lọc
            var panelTop = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FillColor = Color.FromArgb(28, 28, 28),
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10)
            };

            // ComboBox chọn loại báo cáo
            cmbReportType = new Guna2ComboBox
            {
                Size = new Size(120, 36),
                Location = new Point(10, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbReportType.Items.AddRange(new string[] { "Tất cả", "Báo cáo an ninh", "Báo cáo hệ thống", "Báo cáo tuân thủ" });
            cmbReportType.SelectedIndex = 0;
            cmbReportType.SelectedIndexChanged += CmbReportType_SelectedIndexChanged;

            // DateTimePicker chọn ngày bắt đầu
            dtpFrom = new Guna2DateTimePicker
            {
                Size = new Size(120, 36),
                Location = new Point(140, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8,
                Format = DateTimePickerFormat.Short
            };
            dtpFrom.ValueChanged += DtpFrom_ValueChanged;

            // DateTimePicker chọn ngày kết thúc
            dtpTo = new Guna2DateTimePicker
            {
                Size = new Size(120, 36),
                Location = new Point(270, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8,
                Format = DateTimePickerFormat.Short
            };
            dtpTo.ValueChanged += DtpTo_ValueChanged;

            // Nút tạo báo cáo mới
            btnNewReport = new Guna2Button
            {
                Text = "Tạo báo cáo mới",
                Size = new Size(150, 36),
                Location = new Point(400, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnNewReport.Click += BtnNewReport_Click;

            // Nút xuất báo cáo
            btnExport = new Guna2Button
            {
                Text = "Xuất báo cáo",
                Size = new Size(150, 36),
                Location = new Point(560, 7),
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
                Location = new Point(720, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnDelete.Click += BtnDelete_Click;

            // Thêm các control vào panel
            panelTop.Controls.Add(cmbReportType);
            panelTop.Controls.Add(dtpFrom);
            panelTop.Controls.Add(dtpTo);
            panelTop.Controls.Add(btnNewReport);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(btnDelete);

            // Thêm các panel vào control
            panelMain.Controls.Add(gridReports);
            panelMain.Controls.Add(panelTop);
            this.Controls.Add(panelMain);

            // Load dữ liệu
            LoadData();
        }

        private void LoadData()
        {
            // TODO: Load dữ liệu từ database
            gridReports.Rows.Clear();
            gridReports.Rows.Add("1", "Báo cáo an ninh tháng 3", "Báo cáo an ninh", "2025-03-20", "Admin", "Báo cáo tổng hợp các sự cố an ninh");
            gridReports.Rows.Add("2", "Báo cáo hệ thống tuần 12", "Báo cáo hệ thống", "2025-03-19", "User1", "Báo cáo hoạt động hệ thống");
        }

        private void CmbReportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo loại báo cáo
            LoadData();
        }

        private void DtpFrom_ValueChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo ngày bắt đầu
            LoadData();
        }

        private void DtpTo_ValueChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo ngày kết thúc
            LoadData();
        }

        private void BtnNewReport_Click(object sender, EventArgs e)
        {
            // TODO: Mở form tạo báo cáo mới
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // TODO: Xuất báo cáo
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridReports.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa báo cáo này?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Xóa báo cáo
                    gridReports.Rows.RemoveAt(gridReports.SelectedRows[0].Index);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn báo cáo cần xóa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 