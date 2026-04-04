namespace VisualKeyBoard;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private ComboBox cmbPorts = null!;
    private TextBox txtBaud = null!;
    private Button btnRefresh = null!;
    private Button btnConnect = null!;
    private TextBox txtLog = null!;
    private Label lblPort = null!;
    private Label lblBaud = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        cmbPorts = new ComboBox();
        txtBaud = new TextBox();
        btnRefresh = new Button();
        btnConnect = new Button();
        txtLog = new TextBox();
        lblPort = new Label();
        lblBaud = new Label();
        SuspendLayout();
        // 
        // cmbPorts
        // 
        cmbPorts.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPorts.DropDownWidth = 260;
        cmbPorts.Location = new Point(52, 12);
        cmbPorts.Name = "cmbPorts";
        cmbPorts.Size = new Size(180, 25);
        cmbPorts.TabIndex = 1;
        // 
        // txtBaud
        // 
        txtBaud.Location = new Point(290, 12);
        txtBaud.Name = "txtBaud";
        txtBaud.Size = new Size(80, 23);
        txtBaud.TabIndex = 3;
        // 
        // btnRefresh
        // 
        btnRefresh.Location = new Point(380, 11);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(70, 25);
        btnRefresh.TabIndex = 4;
        btnRefresh.Text = "刷新";
        btnRefresh.Click += btnRefresh_Click;
        // 
        // btnConnect
        // 
        btnConnect.Location = new Point(460, 11);
        btnConnect.Name = "btnConnect";
        btnConnect.Size = new Size(110, 25);
        btnConnect.TabIndex = 5;
        btnConnect.Text = "连接";
        btnConnect.Click += btnConnect_Click;
        // 
        // txtLog
        // 
        txtLog.Location = new Point(12, 48);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(558, 350);
        txtLog.TabIndex = 6;
        txtLog.TextChanged += txtLog_TextChanged;
        // 
        // lblPort
        // 
        lblPort.AutoSize = true;
        lblPort.Location = new Point(12, 15);
        lblPort.Name = "lblPort";
        lblPort.Size = new Size(32, 17);
        lblPort.TabIndex = 0;
        lblPort.Text = "串口";
        // 
        // lblBaud
        // 
        lblBaud.AutoSize = true;
        lblBaud.Location = new Point(250, 15);
        lblBaud.Name = "lblBaud";
        lblBaud.Size = new Size(44, 17);
        lblBaud.TabIndex = 2;
        lblBaud.Text = "波特率";
        // 
        // MainForm
        // 
        ClientSize = new Size(582, 410);
        Controls.Add(lblPort);
        Controls.Add(cmbPorts);
        Controls.Add(lblBaud);
        Controls.Add(txtBaud);
        Controls.Add(btnRefresh);
        Controls.Add(btnConnect);
        Controls.Add(txtLog);
        Name = "MainForm";
        Text = "VisualKeyBoard";
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }
}
