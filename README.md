# VisualKeyBoard

VisualKeyBoard 是一个基于 Windows Forms 的串口键盘桥接工具：

- 从串口接收指定格式的数据帧
- 解析按键码
- 通过 Interception 驱动模拟真实硬件键盘按键输入

适用于外设键盘、扫码设备、单片机按键面板等将输入经串口转发到 Windows 应用的场景。

## 功能概览

- 自动刷新并列出系统串口（含设备名）
- 支持自定义波特率连接/断开
- 串口行缓冲解析（按 `\n` 分帧）
- 协议帧校验与日志输出
- 将收到的按键码映射为虚拟键并注入系统输入

## 运行环境

- Windows 10/11
- .NET 8 SDK
- 管理员权限（首次安装 Interception 驱动时需要）

## 快速开始

### 1) 还原并构建

在项目根目录执行：

```powershell
./build.ps1 -Configuration Release
```

或使用 dotnet 命令：

```powershell
dotnet restore VisualKeyBoard.slnx
dotnet build VisualKeyBoard.slnx -c Release
```

### 2) 运行

```powershell
dotnet run --project VisualKeyBoard.csproj -c Release
```

首次运行如果未安装 Interception 驱动，程序会提示安装。请以管理员身份运行程序完成驱动安装后重启应用。

## 串口协议

程序当前识别如下格式：

```text
@NP,KEY=<任意字符串>,CODE=0xHH
```

其中：

- `@NP,` 为固定前缀
- `KEY` 为业务字段（用于日志展示）
- `CODE` 为两位十六进制按键码

示例：

```text
@NP,KEY=NUM1,CODE=0x31
@NP,KEY=BACK,CODE=0x08
@NP,KEY=DOT,CODE=0x2E
```

## 默认按键映射

- `0x30` - `0x39` -> 数字 `0` - `9`
- `0x60` - `0x69` -> 小键盘 `0` - `9`
- `0x08` -> Backspace
- `0x2E` -> 小键盘小数点/删除键映射

未映射码会记录日志，不会发送按键。

## 常见问题

### 1) 提示驱动安装失败

- 确认以管理员身份运行
- 检查安全软件是否拦截
- 安装成功后重启程序

### 2) 串口已连接但没有按键效果

- 确认设备发送的数据帧严格符合协议格式
- 检查 `CODE` 是否在已映射范围内
- 观察窗口日志是否出现“无效帧/未映射”

### 3) 端口列表看不到设备

- 点击界面“刷新串口”
- 检查设备管理器与驱动
- 确认串口未被其他程序占用

## 项目结构

- `Program.cs`：应用入口
- `MainForm.cs`：串口收发、协议解析、按键映射与注入逻辑
- `build.ps1`：还原与构建脚本
- `VisualKeyBoard.csproj`：项目与 NuGet 依赖配置
