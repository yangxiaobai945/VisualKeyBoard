using System.IO.Ports;
using System.Runtime.InteropServices;
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

    private const uint InputKeyboard = 1;
    private const uint KeyEventFKeyUp = 0x0002;

    private const ushort VkBack = 0x08;
    private const ushort Vk0 = 0x30;
    private const ushort Vk9 = 0x39;
    private const ushort VkOemPeriod = 0xBE;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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

        switch (code)
        {
            case 0x08:
                vkCode = VkBack;
                mapName = "BACK";
                return true;
            case 0x2E:
                vkCode = VkOemPeriod;
                mapName = "OEM_PERIOD";
                return true;
            default:
                vkCode = 0;
                mapName = string.Empty;
                return false;
        }
    }

    private static void SendVirtualKey(ushort vkCode)
    {
        INPUT[] inputs =
        [
            new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            },
            new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = 0,
                        dwFlags = KeyEventFKeyUp,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            }
        ];

        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput 失败，Win32Error={err}");
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
