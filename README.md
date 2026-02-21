# TransAnyWhere - Cross-Platform File Transfer

## 🚀 Publish Guide / 发布指南

### For Windows (Single Executable)
To generate a single-file executable with minimized size:
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

### For macOS (Apple Silicon / Intel)
We use Folder mode to avoid permission issues and the NETSDK1099 error:

# Apple Silicon (M1/M2/M3)
dotnet publish -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=true /p:TrimMode=link -o ./dist

# Intel Mac
dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=true /p:TrimMode=link -o ./dist

---

## 🛠️ Key Optimizations / 技术优化

- **Smart Networking**: Prioritizes WLAN/Wi-Fi adapters for stable transfers.
  **智能网络**: 优先识别 WLAN/无线网卡，确保传输稳定。
- **UI/UX**: Native Dark Mode support and cross-platform ComboBox styling.
  **界面优化**: 原生深色模式支持，修复跨平台下拉框样式。
- **Size Reduction**: Compressed from 50MB to 22MB via advanced trimming.
  **体积优化**: 通过裁剪技术将体积从 50MB 压缩至 22MB。

---

## ❓ Troubleshooting / 问题处理

### macOS "App is damaged" or "Cannot open"
If macOS blocks the app, run the following in your terminal:
若 macOS 提示文件损坏或无法打开，请执行：

chmod +x ./TransAnyWhere
xattr -cr ./TransAnyWhere

### NETSDK1099 Build Error
This occurs if SelfContained is not set during trimming. Always use --self-contained true as shown in the commands above.
若编译报 NETSDK1099 错误，请确保在裁剪时开启了“独立部署”参数。

---

## ⚖️ Disclaimer / 免责声明

This software is for educational and personal use only. The developer is not responsible for any data loss, security issues, or damages caused by the use of this tool. Use it at your own risk.
本软件仅供教育与个人使用。开发者不对因使用本工具导致的任何数据丢失、安全问题或损失负责。请在自行承担风险的前提下使用。

---

## 🤝 AI Collaboration / 协作记录

This version (v1.2) was co-developed with Gemini (Including the Readme file).
本版本 (v1.2) 由开发者与 Gemini 共同协作优化 (包括编写此 Readme)。

- Logic: Multi-NIC detection logic. / 逻辑: 多网卡智能识别。
- Build: macOS trimming and size optimization. / 构建: macOS 环境裁剪与体积优化。
- UI: Cross-platform Avalonia styling fixes. / UI: 跨平台 Avalonia 样式修正。

---
© 2026 TransAnyWhere
