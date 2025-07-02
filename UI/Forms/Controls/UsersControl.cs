using System;
using System.Windows.Forms;
using System.Drawing;
using Guna.UI2.WinForms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class UsersControl : UserControl
    {
        private readonly Guna2Panel panelMain;
        private readonly Guna2DataGridView gridUsers;
        private readonly Guna2Button btnNewUser;
        private readonly Guna2Button btnEdit;
        private readonly Guna2Button btnDelete;
        private readonly Guna2ComboBox cmbRole;
        private readonly Guna2TextBox txtSearch;

        public UsersControl()
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

            // Grid hiển thị danh sách người dùng
            gridUsers = new Guna2DataGridView
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
            gridUsers.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Tên đăng nhập", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Họ tên", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Vai trò", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "LastLogin", HeaderText = "Đăng nhập cuối", Width = 150 }
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

            // ComboBox lọc theo vai trò
            cmbRole = new Guna2ComboBox
            {
                Size = new Size(120, 36),
                Location = new Point(220, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            cmbRole.Items.AddRange(new string[] { "Tất cả", "Admin", "User", "Viewer" });
            cmbRole.SelectedIndex = 0;
            cmbRole.SelectedIndexChanged += CmbRole_SelectedIndexChanged;

            // Nút thêm người dùng mới
            btnNewUser = new Guna2Button
            {
                Text = "Thêm người dùng",
                Size = new Size(150, 36),
                Location = new Point(350, 7),
                Font = new Font("Segoe UI", 10),
                FillColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                BorderRadius = 8
            };
            btnNewUser.Click += BtnNewUser_Click;

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
            panelTop.Controls.Add(cmbRole);
            panelTop.Controls.Add(btnNewUser);
            panelTop.Controls.Add(btnEdit);
            panelTop.Controls.Add(btnDelete);

            // Thêm các panel vào control
            panelMain.Controls.Add(gridUsers);
            panelMain.Controls.Add(panelTop);
            this.Controls.Add(panelMain);

            // Load dữ liệu
            LoadData();
        }

        private void LoadData()
        {
            // TODO: Load dữ liệu từ database
            gridUsers.Rows.Clear();
            gridUsers.Rows.Add("1", "admin", "Administrator", "admin@example.com", "Admin", "Hoạt động", "2025-03-20 10:30");
            gridUsers.Rows.Add("2", "user1", "User One", "user1@example.com", "User", "Hoạt động", "2025-03-19 15:45");
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // TODO: Tìm kiếm người dùng
            LoadData();
        }

        private void CmbRole_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TODO: Lọc theo vai trò
            LoadData();
        }

        private void BtnNewUser_Click(object sender, EventArgs e)
        {
            // TODO: Mở form thêm người dùng mới
            MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridUsers.SelectedRows.Count > 0)
            {
                // TODO: Mở form sửa thông tin người dùng
                MessageBox.Show("Chức năng đang phát triển", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn người dùng cần sửa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridUsers.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa người dùng này?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Xóa người dùng
                    gridUsers.Rows.RemoveAt(gridUsers.SelectedRows[0].Index);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn người dùng cần xóa", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 