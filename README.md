# TransAnyWhere - Cross-Platform File Transfer

## 📥 Downloads / 下载地址

**Latest Version (v1.2):**
- **Windows (v1.2)**: [TransAnyWhereApp.Desktop.exe.zip](./TransAnyWhereApp.Desktop.exe.zip)
- **macOS (v1.2)**: [TransAnyWhere.zip](./TransAnyWhere.zip)
- **Linux ARM64 (Raspberry Pi 5)**: [TransAnyWhereApp.linux-arm64.rar](./TransAnyWhereApp.linux-arm64.rar)
- *(Note: Run `chmod +x TransAnyWhere` after extracting / 解压后请执行权限授予命令)*
- **Other Linux**: See [Self-Build section] below.

> **⚠️ Security Note / 安全提示:**
> - SHA-256 checksums are currently not provided. Please verify the source before running.
> - 暂未提供 SHA-256 校验码，请在运行前确认来源安全。

---

## ✨ Features / 核心功能

- **No Client Required**: Access via browser for bi-directional file transfer.
  **免客户端**: 接收端通过浏览器即可实现文件双向传输。
- **Zero Configuration**: No installation required. Run and transfer.
  **零配置**: 即开即用，无需复杂设置。
- **Secure Link**: Connections are only accepted via manual QR/ID authorization.
  **安全连接**: 仅通过二维码/ID 手动授权，拒绝未授权访问。
- **High Compatibility**: Support for Windows, macOS, and **Raspberry Pi (ARM64)**.
  **高兼容性**: 支持 Win, Mac 以及**树莓派 5** 等 ARM64 设备。

---

## 📖 Usage & Security / 使用说明与安全性

- **Connection Authorization**: New connections can only be established when the **QR Code/ID** is displayed.
  **连接授权**: 仅在**显示二维码/ID**时才允许建立新连接，确保安全性。
- **Auto-Reconnect**: Automatically attempts to reconnect if the network drops.
  **自动重连**: 网络波动导致断开后会自动尝试重连。
- **No Config Files**: Currently runs without any config files.
  **无配置文件**: 程序不产生任何配置，如需增加此功能请留言。
- **UI Design**: Functional priority! I am a developer, not a designer.
  **界面美化**: 开发者非美工，以功能实现为主，请多包涵。

---

## 🚀 Build Guide / 编译与发布指南

### 🐧 Linux Build (Linux 自行编译)
Due to environment diversity, Linux users are encouraged to build from source:
(Command: dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true)

### 🪟 Windows (Single Executable)
(Command: dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true)

### 🍎 macOS (Apple Silicon)
(Command: dotnet publish -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=true /p:TrimMode=link -o ./dist)

---

## ❓ Troubleshooting / 问题处理

- **macOS "App is damaged"**: Run 'xattr -cr ./TransAnyWhere' and 'chmod +x ./TransAnyWhere'.
- **NETSDK1099 Error**: Ensure '--self-contained true' is used during publish.

---

## ⚖️ Disclaimer & Contribution / 免责声明与贡献说明

- **Disclaimer**: Use at your own risk. The developer is not responsible for any data loss.
- **Contribution**: Currently personally maintained. External permissions are not yet established.

---

## 🤝 AI Collaboration / 协作记录

This version (v1.2) was co-developed with Gemini (Including this Readme).
本版本 (v1.2) 由开发者与 Gemini 共同协作优化 (包括编写此 Readme)。

---
© 2026 TransAnyWhere
