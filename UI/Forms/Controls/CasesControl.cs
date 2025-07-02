using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class CasesControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2DataGridView gridCases;
        private readonly Guna2Button btnNewCase;
        private readonly Guna2Button btnExport;
        private readonly Guna2Button btnDelete;
        private readonly Guna2ComboBox cmbPriority;
        private readonly Guna2ComboBox cmbStatus;

        public CasesControl()
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

            // Grid hiển thị danh sách vụ việc
            gridCases = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            // Thêm các cột (chỉ set Width cho cột nhỏ nếu cần)
            gridCases.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Tiêu đề" },
                new DataGridViewTextBoxColumn { Name = "Priority", HeaderText = "Độ ưu tiên" },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái" },
                new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Ngày tạo" },
                new DataGridViewTextBoxColumn { Name = "AssignedTo", HeaderText = "Người phụ trách" },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Mô tả" }
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

            // ComboBox lọc theo độ ưu tiên
            cmbPriority = new Guna2ComboBox
            {
                Size = new Size(120, 36),
                Location = new Point(10, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbPriority.Items.AddRange(new string[] { "Tất cả", "Cao", "Trung bình", "Thấp" });
            cmbPriority.SelectedIndex = 0;
            cmbPriority.SelectedIndexChanged += CmbPriority_SelectedIndexChanged;

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
            cmbStatus.Items.AddRange(new string[] { "Tất cả", "Mới", "Đang xử lý", "Đã xử lý", "Đã đóng" });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.SelectedIndexChanged += CmbStatus_SelectedIndexChanged;

            // Nút tạo vụ việc mới
            btnNewCase = new Guna2Button
            {
                Text = "Tạo vụ việc mới",
                Size = new Size(150, 36),
                Location = new Point(270, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnNewCase.Click += BtnNewCase_Click;

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
            panelTop.Controls.Add(cmbPriority);
            panelTop.Controls.Add(cmbStatus);
            panelTop.Controls.Add(btnNewCase);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(btnDelete);

            // Thêm panelTop và gridCases vào panelMain
            panelMain.Controls.Add(gridCases);
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
            gridCases.Rows.Clear();
            gridCases.Rows.Add("1", "Điều tra vi phạm bảo mật", "Cao", "Đang xử lý", "2025-03-20", "Admin", "Phát hiện hoạt động đáng ngờ");
            gridCases.Rows.Add("2", "Kiểm tra hệ thống", "Trung bình", "Mới", "2025-03-19", "User1", "Kiểm tra định kỳ");
        }

        private void CmbPriority_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo độ ưu tiên
            LoadData();
        }

        private void CmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo trạng thái
            LoadData();
        }

        private void BtnNewCase_Click(object sender, EventArgs e)
        {
            // TODO: Mở form tạo vụ việc mới
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // TODO: Xuất báo cáo
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridCases.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa vụ việc này?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Xóa vụ việc
                    gridCases.Rows.RemoveAt(gridCases.SelectedRows[0].Index);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn vụ việc cần xóa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 