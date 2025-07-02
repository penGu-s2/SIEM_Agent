using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class PlaybooksControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2DataGridView gridPlaybooks;
        private readonly Guna2Button btnNewPlaybook;
        private readonly Guna2Button btnEdit;
        private readonly Guna2Button btnDelete;
        private readonly Guna2ComboBox cmbCategory;
        private readonly Guna2TextBox txtSearch;

        public PlaybooksControl()
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

            // Grid hiển thị danh sách playbook
            gridPlaybooks = new Guna2DataGridView
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
            gridPlaybooks.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên playbook", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Danh mục", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Mô tả", Width = 300 },
                new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "Người tạo", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Ngày tạo", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", Width = 100 }
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

            // TextBox tìm kiếm
            txtSearch = new Guna2TextBox
            {
                Size = new Size(200, 36),
                Location = new Point(10, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8,
                PlaceholderText = "Tìm kiếm..."
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            // ComboBox lọc theo danh mục
            cmbCategory = new Guna2ComboBox
            {
                Size = new Size(120, 36),
                Location = new Point(220, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbCategory.Items.AddRange(new string[] { "Tất cả", "Phát hiện xâm nhập", "Phản ứng sự cố", "Tuân thủ" });
            cmbCategory.SelectedIndex = 0;
            cmbCategory.SelectedIndexChanged += CmbCategory_SelectedIndexChanged;

            // Nút tạo playbook mới
            btnNewPlaybook = new Guna2Button
            {
                Text = "Tạo playbook mới",
                Size = new Size(150, 36),
                Location = new Point(350, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnNewPlaybook.Click += BtnNewPlaybook_Click;

            // Nút sửa
            btnEdit = new Guna2Button
            {
                Text = "Sửa",
                Size = new Size(100, 36),
                Location = new Point(510, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnEdit.Click += BtnEdit_Click;

            // Nút xóa
            btnDelete = new Guna2Button
            {
                Text = "Xóa",
                Size = new Size(100, 36),
                Location = new Point(620, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnDelete.Click += BtnDelete_Click;

            // Thêm các control vào panel
            panelTop.Controls.Add(txtSearch);
            panelTop.Controls.Add(cmbCategory);
            panelTop.Controls.Add(btnNewPlaybook);
            panelTop.Controls.Add(btnEdit);
            panelTop.Controls.Add(btnDelete);

            // Thêm các panel vào control
            panelMain.Controls.Add(gridPlaybooks);
            panelMain.Controls.Add(panelTop);
            this.Controls.Add(panelMain);

            // Load dữ liệu
            LoadData();
        }

        private void LoadData()
        {
            // TODO: Load dữ liệu từ database
            gridPlaybooks.Rows.Clear();
            gridPlaybooks.Rows.Add("1", "Phát hiện xâm nhập", "Phát hiện xâm nhập", "Phát hiện và cảnh báo xâm nhập", "Admin", "2025-03-20", "Hoạt động");
            gridPlaybooks.Rows.Add("2", "Phản ứng sự cố", "Phản ứng sự cố", "Quy trình xử lý sự cố", "User1", "2025-03-19", "Hoạt động");
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // TODO: Tìm kiếm playbook
            LoadData();
        }

        private void CmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo danh mục
            LoadData();
        }

        private void BtnNewPlaybook_Click(object sender, EventArgs e)
        {
            // TODO: Mở form tạo playbook mới
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridPlaybooks.SelectedRows.Count > 0)
            {
                // TODO: Mở form sửa playbook
                MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn playbook cần sửa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridPlaybooks.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa playbook này?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Xóa playbook
                    gridPlaybooks.Rows.RemoveAt(gridPlaybooks.SelectedRows[0].Index);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn playbook cần xóa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 