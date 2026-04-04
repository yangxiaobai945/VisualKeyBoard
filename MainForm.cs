using System.IO.Ports;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using InputInterceptorNS;

namespace VisualKeyBoard;

public partial class MainForm : Form
{
    private sealed record SerialPortItem(string PortName, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private SerialPort? _serialPort;
    private readonly object _rxLock = new();
    private readonly StringBuilder _rxBuffer = new();
    private KeyboardHook? _keyboardHook;
    private readonly ContextMenuStrip _txtLogMenu = new();
    private readonly ToolStripMenuItem _txtLogClearItem;
    private readonly ToolStripMenuItem _txtLogCopyItem;
    private readonly ToolStripMenuItem _txtLogSelectAllItem;

    private static readonly Regex NpFrameRegex = new(
        "^@NP,KEY=(.*),CODE=0x([0-9A-Fa-f]{2})$",
        RegexOptions.Compiled);

    private const ushort VkBack = 0x08;
    private const ushort Vk0 = 0x30;
    private const ushort Vk9 = 0x39;
    private const ushort VkNumpad0 = 0x60;
    private const ushort VkNumpad9 = 0x69;

    public MainForm()
    {
        InitializeComponent();
        txtBaud.Text = "115200";

        _txtLogClearItem = new ToolStripMenuItem("清空日志", null, (_, _) => txtLog.Clear());
        _txtLogCopyItem = new ToolStripMenuItem("复制", null, (_, _) => txtLog.Copy());
        _txtLogSelectAllItem = new ToolStripMenuItem("全选", null, (_, _) => txtLog.SelectAll());

        _txtLogMenu.Opening += TxtLogMenu_Opening;
        _txtLogMenu.Items.AddRange(new ToolStripItem[]
        {
            _txtLogClearItem,
            new ToolStripSeparator(),
            _txtLogCopyItem,
            _txtLogSelectAllItem
        });
        txtLog.ContextMenuStrip = _txtLogMenu;

        RefreshPorts();
        UpdateConnectionButtonText();
        this.InitializeInterceptionDriver();
    }

    private void TxtLogMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _txtLogClearItem.Enabled = txtLog.TextLength > 0;
        _txtLogCopyItem.Enabled = txtLog.SelectionLength > 0;
        _txtLogSelectAllItem.Enabled = txtLog.TextLength > 0;
    }

    private void InitializeInterceptionDriver()
    {
        try
        {
            if (!InputInterceptor.CheckDriverInstalled())
            {
                if (!InputInterceptor.CheckAdministratorRights())
                {
                    MessageBox.Show("请以【管理员身份】运行程序来安装驱动！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Log("正在安装 Interception 驱动...");
                if (InputInterceptor.InstallDriver())
                {
                    Log("驱动安装成功！请重启程序后再使用。");
                }
                else
                {
                    Log("驱动安装失败，请检查杀毒软件或手动安装。");
                }
                return;
            }

            InputInterceptor.Initialize();

            _keyboardHook = new KeyboardHook(KeyboardHookCallback);

            Log($"KeyboardHook 状态 - IsInitialized: {_keyboardHook.IsInitialized}, Active: {_keyboardHook.Active}, CanSimulateInput: {_keyboardHook.CanSimulateInput}");
            Log("Interception 驱动已就绪，可模拟真实硬件按键");
        }
        catch (Exception ex)
        {
            Log("驱动初始化失败: " + ex.Message);
        }
    }

    private void btnRefresh_Click(object? sender, EventArgs e)
    {
        RefreshPorts();
    }

    private void btnConnect_Click(object? sender, EventArgs e)
    {
        if (_serialPort is { IsOpen: true })
        {
            ClosePort();
            return;
        }

        string portName = GetSelectedPortName();
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
            UpdateConnectionButtonText();
            Log($"已连接 {portName} @ {baud}");
        }
        catch (Exception ex)
        {
            Log("连接失败: " + ex.Message);
            UpdateConnectionButtonText();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        ClosePort();
    }

    private void RefreshPorts()
    {
        string[] ports = SerialPort.GetPortNames().OrderBy(x => x).ToArray();
        string prevPortName = GetSelectedPortName();

        var items = ports
            .Select(portName => new SerialPortItem(portName, GetPortDisplayName(portName)))
            .ToArray();

        cmbPorts.BeginUpdate();
        try
        {
            cmbPorts.Items.Clear();
            cmbPorts.Items.AddRange(items);

            if (!string.IsNullOrWhiteSpace(prevPortName))
            {
                for (int i = 0; i < cmbPorts.Items.Count; i++)
                {
                    if (cmbPorts.Items[i] is SerialPortItem item && item.PortName == prevPortName)
                    {
                        cmbPorts.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (cmbPorts.SelectedIndex < 0 && cmbPorts.Items.Count > 0)
            {
                cmbPorts.SelectedIndex = 0;
            }
        }
        finally
        {
            cmbPorts.EndUpdate();
        }

        Log("已刷新串口: " + ports.Length);
    }

    private static string GetPortDisplayName(string portName)
    {
        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(" + portName + ")%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                string? name = obj["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }
        catch
        {
        }

        return portName;
    }

    private string GetSelectedPortName()
    {
        if (cmbPorts.SelectedItem is SerialPortItem item)
        {
            return item.PortName;
        }

        string text = cmbPorts.Text.Trim();
        if (text.Length == 0)
        {
            return string.Empty;
        }

        int start = text.LastIndexOf('(');
        int end = text.LastIndexOf(')');
        if (start >= 0 && end > start)
        {
            string candidate = text[(start + 1)..end].Trim();
            if (candidate.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return text;
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

        if (!TryMapVirtualKey(code, out ushort vkCode, out string mapName))
        {
            Log($"未映射 KEY={key}, CODE=0x{code:X2}");
            return;
        }

        BeginInvoke(new Action(() =>
        {
            try
            {
                SendVirtualKey(vkCode);
                Log($"已发送 KEY={key}, CODE=0x{code:X2}, VK={vkCode:X2}({mapName})");
            }
            catch (Exception ex)
            {
                Log("发送失败: " + ex.Message);
            }
        }));
    }

    private static bool TryMapVirtualKey(byte code, out ushort vkCode, out string mapName)
    {
        if (code >= Vk0 && code <= Vk9)
        {
            vkCode = code;
            mapName = ((char)code).ToString();
            return true;
        }
        if (code >= VkNumpad0 && code <= VkNumpad9)
        {
            vkCode = code;
            mapName = "NUMPAD_" + (code - VkNumpad0);
            return true;
        }

        switch (code)
        {
            case 0x08:
                vkCode = VkBack;
                mapName = "BACK";
                return true;
            case 0x2E:
                // map to numpad dot
                vkCode = 0x2E; // VK_DELETE
                mapName = "NUMPAD_DOT";
                return true;
            default:
                vkCode = 0;
                mapName = string.Empty;
                return false;
        }
    }

    private void SendVirtualKey(ushort vkCode)
    {
        if (_keyboardHook == null)
        {
            Log("键盘钩子未初始化");
            return;
        }

        KeyCode key = MapVkToKeyCode(vkCode);
        try
        {
            bool success = _keyboardHook.SimulateKeyPress(key, 30);
            Log($"已发送按键: 0x{vkCode:X2} (Interception成功={success})");
        }
        catch (Exception ex)
        {
            Log($"Interception 异常: {ex.Message}");
        }
    }

    private static KeyCode MapVkToKeyCode(ushort vk)
    {
        switch (vk)
        {
            case 0x08: return KeyCode.Backspace;
            case 0x30: return KeyCode.Numpad0;
            case 0x31: return KeyCode.Numpad1;
            case 0x32: return KeyCode.Numpad2;
            case 0x33: return KeyCode.Numpad3;
            case 0x34: return KeyCode.Numpad4;
            case 0x35: return KeyCode.Numpad5;
            case 0x36: return KeyCode.Numpad6;
            case 0x37: return KeyCode.Numpad7;
            case 0x38: return KeyCode.Numpad8;
            case 0x39: return KeyCode.Numpad9;
            case 0xBE: return KeyCode.Dot;
            //add Numapd0-9
            case 0x60: return KeyCode.Numpad0;
            case 0x61: return KeyCode.Numpad1;
            case 0x62: return KeyCode.Numpad2;
            case 0x63: return KeyCode.Numpad3;
            case 0x64: return KeyCode.Numpad4;
            case 0x65: return KeyCode.Numpad5;
            case 0x66: return KeyCode.Numpad6;
            case 0x67: return KeyCode.Numpad7;
            case 0x68: return KeyCode.Numpad8;
            case 0x69: return KeyCode.Numpad9;
            case 0x2E: return KeyCode.NumpadDelete;
            default: return KeyCode.Escape;
        }
    }

    private void KeyboardHookCallback(ref KeyStroke keyStroke)
    {
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

            UpdateConnectionButtonText();
        }
        catch (Exception ex)
        {
            Log("断开异常: " + ex.Message);
        }
    }

    private void UpdateConnectionButtonText()
    {
        if (btnConnect.IsHandleCreated && btnConnect.InvokeRequired)
        {
            btnConnect.BeginInvoke(new Action(UpdateConnectionButtonText));
            return;
        }

        btnConnect.Text = _serialPort is { IsOpen: true } ? "断开" : "连接";
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

    private void txtLog_TextChanged(object sender, EventArgs e)
    {

    }
}
