using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class ParserControl : UserControl
    {
        private ListView listView;
        private Button btnAdd, btnEdit, btnDelete, btnSave;
        private List<ParserItem> parsers = new List<ParserItem>();
        private string parserFile = "parsers.conf";

        public ParserControl()
        {
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.Dock = DockStyle.Fill;

            listView = new ListView
            {
                Dock = DockStyle.Top,
                Height = 300,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            listView.Columns.Add("Name", 150);
            listView.Columns.Add("Format", 80);
            listView.Columns.Add("Regex", 350);
            listView.Columns.Add("Time_Key", 100);
            listView.Columns.Add("Time_Format", 150);
            listView.Columns.Add("Time_Keep", 80);

            btnAdd = new Button { Text = "Thêm mới", Location = new Point(10, 320), Width = 100, BackColor = Color.FromArgb(79, 142, 247), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnEdit = new Button { Text = "Sửa", Location = new Point(120, 320), Width = 100, BackColor = Color.FromArgb(67, 196, 99), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelete = new Button { Text = "Xóa", Location = new Point(230, 320), Width = 100, BackColor = Color.FromArgb(244, 67, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave = new Button { Text = "Lưu tất cả", Location = new Point(340, 320), Width = 100, BackColor = Color.FromArgb(255, 167, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnAdd.Click += (s, e) => ShowParserForm(null);
            btnEdit.Click += (s, e) =>
            {
                if (listView.SelectedItems.Count > 0)
                {
                    var item = listView.SelectedItems[0];
                    ShowParserForm(parsers[item.Index]);
                }
            };
            btnDelete.Click += (s, e) =>
            {
                if (listView.SelectedItems.Count > 0)
                {
                    var idx = listView.SelectedItems[0].Index;
                    parsers.RemoveAt(idx);
                    RefreshList();
                }
            };
            btnSave.Click += (s, e) => SaveParsersToFile();

            this.Controls.Add(listView);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnSave);

            LoadParsersFromFile();
            RefreshList();
        }

        private void LoadParsersFromFile()
        {
            parsers.Clear();
            if (!File.Exists(parserFile)) return;
            var lines = File.ReadAllLines(parserFile);
            ParserItem current = null;
            foreach (var line in lines)
            {
                var l = line.Trim();
                if (l.Equals("[PARSER]", StringComparison.OrdinalIgnoreCase))
                {
                    if (current != null) parsers.Add(current);
                    current = new ParserItem();
                }
                else if (current != null && l.Contains(" "))
                {
                    var idx = l.IndexOf(' ');
                    var key = l.Substring(0, idx).Trim();
                    var value = l.Substring(idx).Trim();
                    switch (key)
                    {
                        case "Name": current.Name = value; break;
                        case "Format": current.Format = value; break;
                        case "Regex": current.Regex = value; break;
                        case "Time_Key": current.Time_Key = value; break;
                        case "Time_Format": current.Time_Format = value; break;
                        case "Time_Keep": current.Time_Keep = value; break;
                    }
                }
            }
            if (current != null && !string.IsNullOrEmpty(current.Name)) parsers.Add(current);
        }

        private void SaveParsersToFile()
        {
            var lines = new List<string>();
            foreach (var p in parsers)
            {
                lines.Add("[PARSER]");
                lines.Add($"    Name        {p.Name}");
                lines.Add($"    Format      {p.Format}");
                lines.Add($"    Regex       {p.Regex}");
                if (!string.IsNullOrEmpty(p.Time_Key)) lines.Add($"    Time_Key    {p.Time_Key}");
                if (!string.IsNullOrEmpty(p.Time_Format)) lines.Add($"    Time_Format {p.Time_Format}");
                if (!string.IsNullOrEmpty(p.Time_Keep)) lines.Add($"    Time_Keep   {p.Time_Keep}");
                lines.Add("");
            }
            File.WriteAllLines(parserFile, lines);
            MessageBox.Show("Đã lưu parser thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshList()
        {
            listView.Items.Clear();
            foreach (var p in parsers)
            {
                var item = new ListViewItem(new[]
                {
                    p.Name, p.Format, p.Regex, p.Time_Key, p.Time_Format, p.Time_Keep
                });
                listView.Items.Add(item);
            }
        }

        private void ShowParserForm(ParserItem parser)
        {
            var form = new Form
            {
                Text = parser == null ? "Thêm parser" : "Sửa parser",
                Size = new Size(500, 400),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };
            var lblName = new Label { Text = "Name:", Location = new Point(20, 20), AutoSize = true };
            var txtName = new TextBox { Location = new Point(120, 18), Width = 320 };
            var lblFormat = new Label { Text = "Format:", Location = new Point(20, 60), AutoSize = true };
            var txtFormat = new TextBox { Location = new Point(120, 58), Width = 320, Text = "regex" };
            var lblRegex = new Label { Text = "Regex:", Location = new Point(20, 100), AutoSize = true };
            var txtRegex = new TextBox { Location = new Point(120, 98), Width = 320 };
            var lblTimeKey = new Label { Text = "Time_Key:", Location = new Point(20, 140), AutoSize = true };
            var txtTimeKey = new TextBox { Location = new Point(120, 138), Width = 320 };
            var lblTimeFormat = new Label { Text = "Time_Format:", Location = new Point(20, 180), AutoSize = true };
            var txtTimeFormat = new TextBox { Location = new Point(120, 178), Width = 320 };
            var lblTimeKeep = new Label { Text = "Time_Keep:", Location = new Point(20, 220), AutoSize = true };
            var txtTimeKeep = new TextBox { Location = new Point(120, 218), Width = 320 };
            var btnOK = new Button { Text = "OK", Location = new Point(120, 270), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(220, 270), DialogResult = DialogResult.Cancel };

            if (parser != null)
            {
                txtName.Text = parser.Name;
                txtFormat.Text = parser.Format;
                txtRegex.Text = parser.Regex;
                txtTimeKey.Text = parser.Time_Key;
                txtTimeFormat.Text = parser.Time_Format;
                txtTimeKeep.Text = parser.Time_Keep;
            }

            form.Controls.Add(lblName);
            form.Controls.Add(txtName);
            form.Controls.Add(lblFormat);
            form.Controls.Add(txtFormat);
            form.Controls.Add(lblRegex);
            form.Controls.Add(txtRegex);
            form.Controls.Add(lblTimeKey);
            form.Controls.Add(txtTimeKey);
            form.Controls.Add(lblTimeFormat);
            form.Controls.Add(txtTimeFormat);
            form.Controls.Add(lblTimeKeep);
            form.Controls.Add(txtTimeKeep);
            form.Controls.Add(btnOK);
            form.Controls.Add(btnCancel);

            form.AcceptButton = btnOK;
            form.CancelButton = btnCancel;

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtFormat.Text) || string.IsNullOrWhiteSpace(txtRegex.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ Name, Format, Regex!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (parser == null)
                {
                    parsers.Add(new ParserItem
                    {
                        Name = txtName.Text.Trim(),
                        Format = txtFormat.Text.Trim(),
                        Regex = txtRegex.Text.Trim(),
                        Time_Key = txtTimeKey.Text.Trim(),
                        Time_Format = txtTimeFormat.Text.Trim(),
                        Time_Keep = txtTimeKeep.Text.Trim()
                    });
                }
                else
                {
                    parser.Name = txtName.Text.Trim();
                    parser.Format = txtFormat.Text.Trim();
                    parser.Regex = txtRegex.Text.Trim();
                    parser.Time_Key = txtTimeKey.Text.Trim();
                    parser.Time_Format = txtTimeFormat.Text.Trim();
                    parser.Time_Keep = txtTimeKeep.Text.Trim();
                }
                RefreshList();
            }
        }

        private class ParserItem
        {
            public string Name;
            public string Format;
            public string Regex;
            public string Time_Key;
            public string Time_Format;
            public string Time_Keep;
        }
    }
} 