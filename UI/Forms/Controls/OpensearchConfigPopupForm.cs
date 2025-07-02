using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SIEM_Agent.UI.Forms.Controls
{
    public class OpensearchConfigPopupForm : Form
    {
        public Dictionary<string, string> ResultConfig { get; private set; }
        private TextBox txtMatch, txtHost, txtPort, txtUser, txtPass, txtIndex, txtType, txtTls, txtTlsVerify, txtSuppressType, txtLogstash;
        private Button btnSave, btnCancel;

        public OpensearchConfigPopupForm()
        {
            this.Text = "Thêm cấu hình Opensearch";
            this.Size = new Size(500, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 13,
                ColumnCount = 2,
                Padding = new Padding(20),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label { Text = "Match", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 0);
            txtMatch = new TextBox { Text = "*", Width = 220 };
            layout.Controls.Add(txtMatch, 1, 0);

            layout.Controls.Add(new Label { Text = "Host", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 1);
            txtHost = new TextBox { Text = "localhost", Width = 220 };
            layout.Controls.Add(txtHost, 1, 1);

            layout.Controls.Add(new Label { Text = "Port", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 2);
            txtPort = new TextBox { Text = "9200", Width = 220 };
            layout.Controls.Add(txtPort, 1, 2);

            layout.Controls.Add(new Label { Text = "HTTP_User", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 3);
            txtUser = new TextBox { Width = 220 };
            layout.Controls.Add(txtUser, 1, 3);

            layout.Controls.Add(new Label { Text = "HTTP_Passwd", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 4);
            txtPass = new TextBox { Width = 220, UseSystemPasswordChar = true };
            layout.Controls.Add(txtPass, 1, 4);

            layout.Controls.Add(new Label { Text = "Index", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 5);
            txtIndex = new TextBox { Text = "fluentbit-logs", Width = 220 };
            layout.Controls.Add(txtIndex, 1, 5);

            layout.Controls.Add(new Label { Text = "Type", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 6);
            txtType = new TextBox { Text = "_doc", Width = 220 };
            layout.Controls.Add(txtType, 1, 6);

            layout.Controls.Add(new Label { Text = "tls", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 7);
            txtTls = new TextBox { Text = "On", Width = 220 };
            layout.Controls.Add(txtTls, 1, 7);

            layout.Controls.Add(new Label { Text = "tls.verify", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 8);
            txtTlsVerify = new TextBox { Text = "Off", Width = 220 };
            layout.Controls.Add(txtTlsVerify, 1, 8);

            layout.Controls.Add(new Label { Text = "Suppress_Type_Name", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 9);
            txtSuppressType = new TextBox { Text = "On", Width = 220 };
            layout.Controls.Add(txtSuppressType, 1, 9);

            layout.Controls.Add(new Label { Text = "Logstash_Format", Anchor = AnchorStyles.Right, AutoSize = true }, 0, 10);
            txtLogstash = new TextBox { Text = "On", Width = 220 };
            layout.Controls.Add(txtLogstash, 1, 10);

            btnSave = new Button { Text = "Lưu", Width = 100 };
            btnCancel = new Button { Text = "Hủy", Width = 100, DialogResult = DialogResult.Cancel };
            var panelBtn = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoSize = true };
            panelBtn.Controls.Add(btnSave);
            panelBtn.Controls.Add(btnCancel);
            layout.Controls.Add(panelBtn, 1, 11);

            btnSave.Click += (s, e) => SaveConfig();
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(layout);
        }

        private void SaveConfig()
        {
            ResultConfig = new Dictionary<string, string>
            {
                ["Match"] = txtMatch.Text.Trim(),
                ["Host"] = txtHost.Text.Trim(),
                ["Port"] = txtPort.Text.Trim(),
                ["HTTP_User"] = txtUser.Text.Trim(),
                ["HTTP_Passwd"] = txtPass.Text.Trim(),
                ["Index"] = txtIndex.Text.Trim(),
                ["Type"] = txtType.Text.Trim(),
                ["tls"] = txtTls.Text.Trim(),
                ["tls.verify"] = txtTlsVerify.Text.Trim(),
                ["Suppress_Type_Name"] = txtSuppressType.Text.Trim(),
                ["Logstash_Format"] = txtLogstash.Text.Trim(),
            };
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
} 