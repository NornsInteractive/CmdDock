# Privacy Policy for CmdDock (CmdDock 隐私政策)

**Last Updated / 最近更新日期**: September 2026

---

## English Version

### 1. Introduction
CmdDock ("the Application", "we", "us", or "our") is a desktop productivity utility and Windows 11 Widget designed for executing user-defined commands, scripts, and automation workflows. We are committed to respecting and protecting your privacy.

This Privacy Policy explains how CmdDock handles your data. In short: **CmdDock does not collect, store, transmit, sell, or share any personal data.**

### 2. Data Collection and Usage
- **No Personal Information Collected**: CmdDock does not require account registration, login credentials, or personal identification. We do not collect your name, email address, IP address, device identifier, or location.
- **Local Storage Only**: All user configurations, custom commands, categories, and execution logs are stored locally on your device in the standard `%LOCALAPPDATA%\CmdDock` folder.
- **No Telemetry or Tracking**: The Application does not contain any analytics SDKs, advertising frameworks, background tracking beacons, or telemetry probes.

### 3. Command Execution and System Permissions
- **Full Trust Capability (`runFullTrust`)**: CmdDock requires full trust capability solely to execute the scripts and terminal commands explicitly configured by the user (such as PowerShell, CMD, or WSL scripts).
- **User Discretion**: CmdDock never executes any command without user initiation or explicit confirmation (when secondary confirmation is enabled). The commands executed are entirely defined and controlled by the user.

### 4. Network Connections
- The Application itself does not communicate with any external cloud servers or APIs.
- Any network activity occurs solely if a user-configured command explicitly initiates network operations (e.g., `ping`, `curl`, or custom network diagnostic scripts).

### 5. Data Deletion
Since all data is stored strictly on your local machine, uninstalling CmdDock through Windows Settings or Microsoft Store will allow you to delete all associated local application files at `%LOCALAPPDATA%\CmdDock`.

### 6. Contact Us
If you have any questions, feedback, or concerns regarding this Privacy Policy, please contact us via our GitHub repository or support channels.

---

## 中文版本

### 1. 概述
CmdDock（以下简称“本应用”或“我们”）是一款专为 Windows 11 打造的快捷命令启动坞与桌面小组件工具，用于协助用户便捷执行常用脚本和自动化任务。我们高度重视用户的隐私安全与数据自主权。

本隐私政策说明了 CmdDock 处理数据的方式。简而言之：**CmdDock 不收集、不存储、不传输、不出售任何个人隐私数据。**

### 2. 数据收集与使用
- **不收集个人信息**：本应用无需注册账号或登录，不收集您的姓名、邮箱、IP 地址、设备标识符或地理位置。
- **仅本地化存储**：用户创建的快捷命令、分类分组配置、小组件偏好设置以及本地运行日志，全部保存在您计算机本地的 `%LOCALAPPDATA%\CmdDock` 目录下。
- **无任何遥测或追踪**：本应用未集成任何第三方数据分析 SDK、广告追踪插件或后台遥测上报代码。

### 3. 命令执行与系统权限
- **完全信任权限（`runFullTrust`）**：本应用申请该权限仅用于按用户指令启动本地命令行解释器（如 PowerShell、CMD、WSL 等）以执行用户自行设定的命令与脚本。
- **用户完全自主控制**：本应用绝不会在后台静默执行任何未经用户触发的指令。对于关键系统操作，提供内嵌二次确认保护机制。

### 4. 网络访问
- 本应用自身不设任何远程服务器通信，不上传任何数据。
- 仅当用户所配置并执行的脚本自身包含网络通信逻辑（例如网络诊断 `ping` 或自定义 API 调用）时，才会产生相应的系统网络行为。

### 5. 数据删除与注销
所有数据均存储在您的本地计算机中。您可以通过 Windows“设置”或应用商店直接卸载本应用，并可随时手动清空 `%LOCALAPPDATA%\CmdDock` 文件夹以彻底清除所有历史数据。

### 6. 联系我们
如果您对本隐私政策有任何疑问、意见或建议，请通过项目的开源仓库 Issues 或技术支持渠道与我们取得联系。
