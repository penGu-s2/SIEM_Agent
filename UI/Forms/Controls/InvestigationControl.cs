using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class InvestigationControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2DataGridView gridInvestigation;
        private readonly Guna2Button btnNewInvestigation;
        private readonly Guna2Button btnExport;
        private readonly Guna2Button btnDelete;

        public InvestigationControl()
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

            // Grid hiển thị danh sách điều tra
            gridInvestigation = new Guna2DataGridView
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
            gridInvestigation.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Tiêu đề" },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái" },
                new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Ngày tạo" },
                new DataGridViewTextBoxColumn { Name = "AssignedTo", HeaderText = "Người phụ trách" }
            });

            // Panel chứa các nút
            var panelButtons = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FillColor = Color.FromArgb(28, 28, 28),
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Nút tạo điều tra mới
            btnNewInvestigation = new Guna2Button
            {
                Text = "Tạo điều tra mới",
                Size = new Size(150, 36),
                Location = new Point(10, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnNewInvestigation.Click += BtnNewInvestigation_Click;

            // Nút xuất báo cáo
            btnExport = new Guna2Button
            {
                Text = "Xuất báo cáo",
                Size = new Size(150, 36),
                Location = new Point(170, 7),
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
                Location = new Point(330, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnDelete.Click += BtnDelete_Click;

            // Thêm các control vào panel
            panelButtons.Controls.Add(btnNewInvestigation);
            panelButtons.Controls.Add(btnExport);
            panelButtons.Controls.Add(btnDelete);

            // Thêm panelButtons và gridInvestigation vào panelMain
            panelMain.Controls.Add(gridInvestigation);
            panelMain.Controls.Add(panelButtons);

            // Đảm bảo chỉ có panelMain trong this.Controls
            this.Controls.Clear();
            this.Controls.Add(panelMain);

            // Load dữ liệu
            LoadData();
        }

        private void LoadData()
        {
            // TODO: Load dữ liệu từ database
            gridInvestigation.Rows.Clear();
            gridInvestigation.Rows.Add("1", "Điều tra vi phạm bảo mật", "Đang thực hiện", "2025-03-20", "Admin");
            gridInvestigation.Rows.Add("2", "Kiểm tra hệ thống", "Hoàn thành", "2025-03-19", "User1");
        }

        private void BtnNewInvestigation_Click(object sender, EventArgs e)
        {
            // TODO: Mở form tạo điều tra mới
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // TODO: Xuất báo cáo
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridInvestigation.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa điều tra này?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Xóa điều tra
                    gridInvestigation.Rows.RemoveAt(gridInvestigation.SelectedRows[0].Index);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn điều tra cần xóa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 