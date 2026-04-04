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
        components = new System.ComponentModel.Container();

        cmbPorts = new ComboBox();
        txtBaud = new TextBox();
        btnRefresh = new Button();
        btnConnect = new Button();
        txtLog = new TextBox();
        lblPort = new Label();
        lblBaud = new Label();

        SuspendLayout();

        lblPort.Text = "串口";
        lblPort.Location = new Point(12, 15);
        lblPort.AutoSize = true;

        cmbPorts.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPorts.Location = new Point(52, 12);
        cmbPorts.Size = new Size(120, 23);

        lblBaud.Text = "波特率";
        lblBaud.Location = new Point(184, 15);
        lblBaud.AutoSize = true;

        txtBaud.Location = new Point(236, 12);
        txtBaud.Size = new Size(90, 23);

        btnRefresh.Text = "刷新";
        btnRefresh.Location = new Point(336, 11);
        btnRefresh.Size = new Size(70, 25);
        btnRefresh.Click += btnRefresh_Click;

        btnConnect.Text = "连接";
        btnConnect.Location = new Point(416, 11);
        btnConnect.Size = new Size(150, 25);
        btnConnect.Click += btnConnect_Click;

        txtLog.Location = new Point(12, 48);
        txtLog.Size = new Size(554, 350);
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;

        ClientSize = new Size(578, 410);
        Controls.Add(lblPort);
        Controls.Add(cmbPorts);
        Controls.Add(lblBaud);
        Controls.Add(txtBaud);
        Controls.Add(btnRefresh);
        Controls.Add(btnConnect);
        Controls.Add(txtLog);

        Text = "VisualKeyBoard";
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }
}
