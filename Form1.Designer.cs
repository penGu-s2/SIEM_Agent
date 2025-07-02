namespace SIEM_Agent;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.TextBox txtLogPath;
    private System.Windows.Forms.TextBox txtOpenSearchEndpoint;
    private System.Windows.Forms.TextBox txtUsername;
    private System.Windows.Forms.TextBox txtPassword;
    private System.Windows.Forms.TextBox txtCAFile;
    private System.Windows.Forms.TextBox txtCertFile;
    private System.Windows.Forms.TextBox txtKeyFile;
    private System.Windows.Forms.ComboBox cmbInterval;
    private System.Windows.Forms.ComboBox cmbLogType;
    private System.Windows.Forms.Button btnSaveConfig;
    private System.Windows.Forms.Button btnStart;
    private System.Windows.Forms.Button btnStop;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Label lblLogPath;
    private System.Windows.Forms.Label lblEndpoint;
    private System.Windows.Forms.Label lblUser;
    private System.Windows.Forms.Label lblPass;
    private System.Windows.Forms.Label lblCA;
    private System.Windows.Forms.Label lblCert;
    private System.Windows.Forms.Label lblKey;
    private System.Windows.Forms.Label lblInterval;
    private System.Windows.Forms.Label lblLogType;
    private System.Windows.Forms.Button btnViewLog;
    private System.Windows.Forms.TextBox txtLogResult;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.txtLogPath = new System.Windows.Forms.TextBox();
        this.txtOpenSearchEndpoint = new System.Windows.Forms.TextBox();
        this.txtUsername = new System.Windows.Forms.TextBox();
        this.txtPassword = new System.Windows.Forms.TextBox();
        this.txtCAFile = new System.Windows.Forms.TextBox();
        this.txtCertFile = new System.Windows.Forms.TextBox();
        this.txtKeyFile = new System.Windows.Forms.TextBox();
        this.cmbInterval = new System.Windows.Forms.ComboBox();
        this.cmbLogType = new System.Windows.Forms.ComboBox();
        this.btnSaveConfig = new System.Windows.Forms.Button();
        this.btnStart = new System.Windows.Forms.Button();
        this.btnStop = new System.Windows.Forms.Button();
        this.lblStatus = new System.Windows.Forms.Label();
        this.lblLogPath = new System.Windows.Forms.Label();
        this.lblEndpoint = new System.Windows.Forms.Label();
        this.lblUser = new System.Windows.Forms.Label();
        this.lblPass = new System.Windows.Forms.Label();
        this.lblCA = new System.Windows.Forms.Label();
        this.lblCert = new System.Windows.Forms.Label();
        this.lblKey = new System.Windows.Forms.Label();
        this.lblInterval = new System.Windows.Forms.Label();
        this.lblLogType = new System.Windows.Forms.Label();
        this.btnViewLog = new System.Windows.Forms.Button();
        this.txtLogResult = new System.Windows.Forms.TextBox();

        this.cmbLogType.Items.AddRange(new object[] { "File", "WinlogEvent" });
        this.cmbLogType.Location = new System.Drawing.Point(150, 20);
        this.cmbLogType.Name = "cmbLogType";
        this.cmbLogType.Size = new System.Drawing.Size(200, 23);
        this.cmbLogType.TabIndex = 0;

        this.lblLogType.AutoSize = true;
        this.lblLogType.Location = new System.Drawing.Point(20, 23);
        this.lblLogType.Name = "lblLogType";
        this.lblLogType.Size = new System.Drawing.Size(100, 15);
        this.lblLogType.TabIndex = 1;
        this.lblLogType.Text = "Loại log:";

        this.txtLogPath.Location = new System.Drawing.Point(150, 50);
        this.txtLogPath.Name = "txtLogPath";
        this.txtLogPath.Size = new System.Drawing.Size(200, 23);
        this.txtLogPath.TabIndex = 2;

        this.lblLogPath.AutoSize = true;
        this.lblLogPath.Location = new System.Drawing.Point(20, 53);
        this.lblLogPath.Name = "lblLogPath";
        this.lblLogPath.Size = new System.Drawing.Size(100, 15);
        this.lblLogPath.TabIndex = 3;
        this.lblLogPath.Text = "Đường dẫn log:";

        this.txtOpenSearchEndpoint.Location = new System.Drawing.Point(150, 80);
        this.txtOpenSearchEndpoint.Name = "txtOpenSearchEndpoint";
        this.txtOpenSearchEndpoint.Size = new System.Drawing.Size(200, 23);
        this.txtOpenSearchEndpoint.TabIndex = 4;

        this.lblEndpoint.AutoSize = true;
        this.lblEndpoint.Location = new System.Drawing.Point(20, 83);
        this.lblEndpoint.Name = "lblEndpoint";
        this.lblEndpoint.Size = new System.Drawing.Size(100, 15);
        this.lblEndpoint.TabIndex = 5;
        this.lblEndpoint.Text = "OpenSearch Endpoint:";

        this.txtUsername.Location = new System.Drawing.Point(150, 110);
        this.txtUsername.Name = "txtUsername";
        this.txtUsername.Size = new System.Drawing.Size(200, 23);
        this.txtUsername.TabIndex = 6;

        this.lblUser.AutoSize = true;
        this.lblUser.Location = new System.Drawing.Point(20, 113);
        this.lblUser.Name = "lblUser";
        this.lblUser.Size = new System.Drawing.Size(100, 15);
        this.lblUser.TabIndex = 7;
        this.lblUser.Text = "Username:";

        this.txtPassword.Location = new System.Drawing.Point(150, 140);
        this.txtPassword.Name = "txtPassword";
        this.txtPassword.Size = new System.Drawing.Size(200, 23);
        this.txtPassword.TabIndex = 8;

        this.lblPass.AutoSize = true;
        this.lblPass.Location = new System.Drawing.Point(20, 143);
        this.lblPass.Name = "lblPass";
        this.lblPass.Size = new System.Drawing.Size(100, 15);
        this.lblPass.TabIndex = 9;
        this.lblPass.Text = "Password:";

        this.txtCAFile.Location = new System.Drawing.Point(150, 170);
        this.txtCAFile.Name = "txtCAFile";
        this.txtCAFile.Size = new System.Drawing.Size(200, 23);
        this.txtCAFile.TabIndex = 10;

        this.lblCA.AutoSize = true;
        this.lblCA.Location = new System.Drawing.Point(20, 173);
        this.lblCA.Name = "lblCA";
        this.lblCA.Size = new System.Drawing.Size(100, 15);
        this.lblCA.TabIndex = 11;
        this.lblCA.Text = "CA File:";

        this.txtCertFile.Location = new System.Drawing.Point(150, 200);
        this.txtCertFile.Name = "txtCertFile";
        this.txtCertFile.Size = new System.Drawing.Size(200, 23);
        this.txtCertFile.TabIndex = 12;

        this.lblCert.AutoSize = true;
        this.lblCert.Location = new System.Drawing.Point(20, 203);
        this.lblCert.Name = "lblCert";
        this.lblCert.Size = new System.Drawing.Size(100, 15);
        this.lblCert.TabIndex = 13;
        this.lblCert.Text = "Cert File:";

        this.txtKeyFile.Location = new System.Drawing.Point(150, 230);
        this.txtKeyFile.Name = "txtKeyFile";
        this.txtKeyFile.Size = new System.Drawing.Size(200, 23);
        this.txtKeyFile.TabIndex = 14;

        this.lblKey.AutoSize = true;
        this.lblKey.Location = new System.Drawing.Point(20, 233);
        this.lblKey.Name = "lblKey";
        this.lblKey.Size = new System.Drawing.Size(100, 15);
        this.lblKey.TabIndex = 15;
        this.lblKey.Text = "Key File:";

        this.cmbInterval.Items.AddRange(new object[] { "5", "10", "15", "30", "60" });
        this.cmbInterval.Location = new System.Drawing.Point(150, 260);
        this.cmbInterval.Name = "cmbInterval";
        this.cmbInterval.Size = new System.Drawing.Size(200, 23);
        this.cmbInterval.TabIndex = 16;

        this.lblInterval.AutoSize = true;
        this.lblInterval.Location = new System.Drawing.Point(20, 263);
        this.lblInterval.Name = "lblInterval";
        this.lblInterval.Size = new System.Drawing.Size(100, 15);
        this.lblInterval.TabIndex = 17;
        this.lblInterval.Text = "Interval (phút):";

        this.btnSaveConfig.Location = new System.Drawing.Point(20, 300);
        this.btnSaveConfig.Name = "btnSaveConfig";
        this.btnSaveConfig.Size = new System.Drawing.Size(100, 30);
        this.btnSaveConfig.TabIndex = 18;
        this.btnSaveConfig.Text = "Lưu cấu hình";
        this.btnSaveConfig.Click += new System.EventHandler(this.btnSaveConfig_Click);

        this.btnStart.Location = new System.Drawing.Point(150, 300);
        this.btnStart.Name = "btnStart";
        this.btnStart.Size = new System.Drawing.Size(100, 30);
        this.btnStart.TabIndex = 19;
        this.btnStart.Text = "Bắt đầu";
        this.btnStart.Click += new System.EventHandler(this.btnStart_Click);

        this.btnStop.Location = new System.Drawing.Point(280, 300);
        this.btnStop.Name = "btnStop";
        this.btnStop.Size = new System.Drawing.Size(100, 30);
        this.btnStop.TabIndex = 20;
        this.btnStop.Text = "Dừng";
        this.btnStop.Click += new System.EventHandler(this.btnStop_Click);

        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new System.Drawing.Point(20, 350);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(100, 15);
        this.lblStatus.TabIndex = 21;
        this.lblStatus.Text = "Trạng thái:";

        this.btnViewLog.Location = new System.Drawing.Point(400, 300);
        this.btnViewLog.Name = "btnViewLog";
        this.btnViewLog.Size = new System.Drawing.Size(100, 30);
        this.btnViewLog.TabIndex = 22;
        this.btnViewLog.Text = "Xem log";
        this.btnViewLog.Click += new System.EventHandler(this.btnViewLog_Click);

        this.txtLogResult.Location = new System.Drawing.Point(20, 380);
        this.txtLogResult.Name = "txtLogResult";
        this.txtLogResult.Size = new System.Drawing.Size(600, 120);
        this.txtLogResult.Multiline = true;
        this.txtLogResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtLogResult.ReadOnly = true;
        this.txtLogResult.TabIndex = 23;
        this.txtLogResult.Font = new System.Drawing.Font("Consolas", 9);

        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(650, 520);
        this.Text = "SIEM Agent";
        this.Controls.Add(this.cmbLogType);
        this.Controls.Add(this.lblLogType);
        this.Controls.Add(this.txtLogPath);
        this.Controls.Add(this.txtOpenSearchEndpoint);
        this.Controls.Add(this.txtUsername);
        this.Controls.Add(this.txtPassword);
        this.Controls.Add(this.txtCAFile);
        this.Controls.Add(this.txtCertFile);
        this.Controls.Add(this.txtKeyFile);
        this.Controls.Add(this.cmbInterval);
        this.Controls.Add(this.btnSaveConfig);
        this.Controls.Add(this.btnStart);
        this.Controls.Add(this.btnStop);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.lblLogPath);
        this.Controls.Add(this.lblEndpoint);
        this.Controls.Add(this.lblUser);
        this.Controls.Add(this.lblPass);
        this.Controls.Add(this.lblCA);
        this.Controls.Add(this.lblCert);
        this.Controls.Add(this.lblKey);
        this.Controls.Add(this.lblInterval);
        this.Controls.Add(this.btnViewLog);
        this.Controls.Add(this.txtLogResult);
    }

    #endregion
}
