using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class HttpConfigPopupForm : Form
    {
        public Dictionary<string, string> ResultConfig { get; private set; } = null;
        private TextBox txtHost, txtPort, txtUri, txtFormat, txtJsonDateKey, txtJsonDateFormat;
        private Button btnSave, btnCancel;

        public HttpConfigPopupForm(string inputName, Dictionary<string, string> oldConfig = null)
        {
            this.Text = $"Cấu hình HTTP cho nguồn: {inputName}";
            this.Size = new Size(420, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 2, Padding = new Padding(10) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // hàng nút

            layout.Controls.Add(new Label { Text = "Host", ForeColor = Color.Black, AutoSize = true }, 0, 0);
            txtHost = new TextBox { Width = 200 };
            layout.Controls.Add(txtHost, 1, 0);

            layout.Controls.Add(new Label { Text = "Port", ForeColor = Color.Black, AutoSize = true }, 0, 1);
            txtPort = new TextBox { Width = 80 };
            layout.Controls.Add(txtPort, 1, 1);

            layout.Controls.Add(new Label { Text = "URI", ForeColor = Color.Black, AutoSize = true }, 0, 2);
            txtUri = new TextBox { Width = 200 };
            layout.Controls.Add(txtUri, 1, 2);

            layout.Controls.Add(new Label { Text = "Format", ForeColor = Color.Black, AutoSize = true }, 0, 3);
            txtFormat = new TextBox { Width = 100, Text = "json" };
            layout.Controls.Add(txtFormat, 1, 3);

            layout.Controls.Add(new Label { Text = "Json_date_key", ForeColor = Color.Black, AutoSize = true }, 0, 4);
            txtJsonDateKey = new TextBox { Width = 100, Text = "timestamp" };
            layout.Controls.Add(txtJsonDateKey, 1, 4);

            layout.Controls.Add(new Label { Text = "Json_date_format", ForeColor = Color.Black, AutoSize = true }, 0, 5);
            txtJsonDateFormat = new TextBox { Width = 100, Text = "iso8601" };
            layout.Controls.Add(txtJsonDateFormat, 1, 5);

            btnSave = new Button { Text = "Lưu", DialogResult = DialogResult.OK, AutoSize = true };
            btnCancel = new Button { Text = "Đóng", DialogResult = DialogResult.Cancel, AutoSize = true };
            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);
            layout.Controls.Add(btnPanel, 0, 6);
            layout.SetColumnSpan(btnPanel, 2);

            this.Controls.Add(layout);

            if (oldConfig != null)
            {
                txtHost.Text = oldConfig.ContainsKey("Host") ? oldConfig["Host"] : "";
                txtPort.Text = oldConfig.ContainsKey("Port") ? oldConfig["Port"] : "";
                txtUri.Text = oldConfig.ContainsKey("URI") ? oldConfig["URI"] : "";
                txtFormat.Text = oldConfig.ContainsKey("Format") ? oldConfig["Format"] : "json";
                txtJsonDateKey.Text = oldConfig.ContainsKey("Json_date_key") ? oldConfig["Json_date_key"] : "timestamp";
                txtJsonDateFormat.Text = oldConfig.ContainsKey("Json_date_format") ? oldConfig["Json_date_format"] : "iso8601";
            }

            btnSave.Click += (s, e) =>
            {
                ResultConfig = new Dictionary<string, string>
                {
                    {"Host", txtHost.Text},
                    {"Port", txtPort.Text},
                    {"URI", txtUri.Text},
                    {"Format", txtFormat.Text},
                    {"Json_date_key", txtJsonDateKey.Text},
                    {"Json_date_format", txtJsonDateFormat.Text}
                };
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
        }
    }
} 