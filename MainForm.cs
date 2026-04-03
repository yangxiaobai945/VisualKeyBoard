using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace VisualKeyBoard;

public partial class MainForm : Form
{
    private SerialPort? _serialPort;
    private readonly object _rxLock = new();
    private readonly StringBuilder _rxBuffer = new();

    private static readonly Regex NpFrameRegex = new(
        "^@NP,KEY=(.*),CODE=0x([0-9A-Fa-f]{2})$",
        RegexOptions.Compiled);

    public MainForm()
    {
        InitializeComponent();
        txtBaud.Text = "115200";
        RefreshPorts();
    }

    private void btnRefresh_Click(object? sender, EventArgs e)
    {
        RefreshPorts();
    }

    private void btnConnect_Click(object? sender, EventArgs e)
    {
        if (_serialPort is { IsOpen: true })
        {
            Log("串口已连接");
            return;
        }

        string portName = cmbPorts.Text.Trim();
        if (string.IsNullOrWhiteSpace(portName))
        {
            Log("请先选择串口");
            return;
        }

        if (!int.TryParse(txtBaud.Text.Trim(), out int baud) || baud <= 0)
        {
            Log("波特率无效");
            return;
        }

        try
        {
            _serialPort = new SerialPort(portName, baud)
            {
                Encoding = Encoding.ASCII,
                NewLine = "\n"
            };
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            Log($"已连接 {portName} @ {baud}");
        }
        catch (Exception ex)
        {
            Log("连接失败: " + ex.Message);
        }
    }

    private void btnDisconnect_Click(object? sender, EventArgs e)
    {
        ClosePort();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        ClosePort();
    }

    private void RefreshPorts()
    {
        string[] ports = SerialPort.GetPortNames().OrderBy(x => x).ToArray();
        string prev = cmbPorts.Text;

        cmbPorts.Items.Clear();
        cmbPorts.Items.AddRange(ports);

        if (!string.IsNullOrWhiteSpace(prev) && ports.Contains(prev))
        {
            cmbPorts.Text = prev;
        }
        else if (ports.Length > 0)
        {
            cmbPorts.SelectedIndex = 0;
        }

        Log("已刷新串口: " + ports.Length);
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        string chunk;
        try
        {
            chunk = _serialPort?.ReadExisting() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log("读取异常: " + ex.Message);
            return;
        }

        if (chunk.Length == 0)
        {
            return;
        }

        List<string> lines = new();
        lock (_rxLock)
        {
            _rxBuffer.Append(chunk);
            while (TryPopLine(out string line))
            {
                lines.Add(line);
            }
        }

        foreach (string line in lines)
        {
            ProcessLine(line);
        }
    }

    private bool TryPopLine(out string line)
    {
        for (int i = 0; i < _rxBuffer.Length; i++)
        {
            if (_rxBuffer[i] == '\n')
            {
                line = _rxBuffer.ToString(0, i + 1);
                _rxBuffer.Remove(0, i + 1);
                return true;
            }
        }

        line = string.Empty;
        return false;
    }

    private void ProcessLine(string raw)
    {
        string line = raw.TrimEnd('\r', '\n');

        if (!line.StartsWith("@NP,"))
        {
            return;
        }

        Match m = NpFrameRegex.Match(line);
        if (!m.Success)
        {
            Log("无效帧: " + line);
            return;
        }

        string key = m.Groups[1].Value;
        string hex = m.Groups[2].Value;

        if (!byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte code))
        {
            Log("CODE解析失败: " + line);
            return;
        }

        if (!TryMapSendKeys(code, out string token))
        {
            Log($"未映射 KEY={key}, CODE=0x{code:X2}");
            return;
        }

        BeginInvoke(new Action(() =>
        {
            try
            {
                SendKeys.SendWait(token);
                Log($"已发送 KEY={key}, CODE=0x{code:X2}, SEND={token}");
            }
            catch (Exception ex)
            {
                Log("发送失败: " + ex.Message);
            }
        }));
    }

    private static bool TryMapSendKeys(byte code, out string token)
    {
        if (code >= 0x30 && code <= 0x39)
        {
            token = ((char)code).ToString();
            return true;
        }

        switch (code)
        {
            case 0x08:
                token = "{BACKSPACE}";
                return true;
            case 0x2E:
                token = ".";
                return true;
            default:
                token = string.Empty;
                return false;
        }
    }

    private void ClosePort()
    {
        try
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
                Log("串口已断开");
            }
        }
        catch (Exception ex)
        {
            Log("断开异常: " + ex.Message);
        }
    }

    private void Log(string msg)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(Log), msg);
            return;
        }

        txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {msg}{Environment.NewLine}");
    }
}
