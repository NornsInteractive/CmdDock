# CmdDock (快捷命令启动坞)

> 专为 Windows 11 打造的快捷命令小组件，常驻任务栏左下角小组件面板，支持一键触发系统运维与开发脚本，支持上架微软商店 (Microsoft Store)。

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-1.5%2B-blueviolet.svg)](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 🌟 核心特性

- ⚡ **Win11 左下角原生小组件**：无缝接入 Windows 11 Widget Board (`Win + W`)，不占桌面，一触即发。
- 📐 **自适应多尺寸规格**：
  - **小号 (Small - 1x1)**：精简高频命令，单指速触。
  - **中号 (Medium - 2x1)**：常用双列快捷面板，含即时执行状态反馈。
  - **大号 (Large - 2x2)**：全功能控制面板与分类分组。
- 🛠️ **多引擎脚本执行**：
  - **PowerShell** / **CMD** / **WSL (Linux Bash)** / **可执行程序 (Exe)** / **系统 URL 协议**。
  - 支持**后台静默执行**（捕获日志与返回值）、**弹出独立终端窗口**、**管理员提权运行 (RunAs Admin)**。
- 🛡️ **安全与误触防护**：高危系统操作支持二次确认保护，完全符合微软商店安全审核标准。
- 🎨 **现代 Fluent Design 界面**：WinUI 3 编写的管理主程序，支持 Mica 云母透明材质、深浅色模式自适应。
- 📦 **开箱即用预设库**：内置一键刷新 DNS、重启 Explorer、清理剪贴板、端口排查、Docker 状态、一键锁屏等实用脚本。

---

## 🏗️ 项目架构

```
cmddock/
├── src/
│   ├── CmdDock.Core/      # 核心模型与服务层（命令CRUD、进程执行引擎、日志、预设库）
│   ├── CmdDock.Widget/    # Windows 11 小组件 COM 服务端 (IWidgetProvider2 / Adaptive Cards)
│   └── CmdDock.App/       # WinUI 3 桌面管理应用与 MSIX 打包配置
├── tests/
│   └── CmdDock.Tests/     # xUnit 单元测试套件（执行引擎、卡片生成器、存储测试）
└── cmddock.sln            # 统一工程解决方案
```

---

## 🚀 编译与构建说明

本项目使用独立的便携式开发环境（如位于 `D:\Data\Env\dotnet`），**未向系统写入任何永久环境变量**。

### 1. 编译解决方案
```powershell
& "D:\Data\Env\dotnet\dotnet.exe" build cmddock.sln
```

### 2. 运行单元测试
```powershell
& "D:\Data\Env\dotnet\dotnet.exe" test tests\CmdDock.Tests\CmdDock.Tests.csproj
```

### 3. 启动管理端应用
```powershell
& "D:\Data\Env\dotnet\dotnet.exe" run --project src\CmdDock.App\CmdDock.App.csproj
```

---

## 📌 Windows 11 小组件钉选方法

1. 编译并部署应用安装包（MSIX）。
2. 按下快捷键 `Win + W` 或点击任务栏左下角的天气/小组件图标呼出小组件面板。
3. 点击右上角的用户头像或 `+`（添加小组件）按钮。
4. 在可用组件列表中找到 **CmdDock**，点击固定 (Pin)。
5. 点击卡片右上角的三点菜单 (`...`) 可自由切换小、中、大三种尺寸，点击卡片中的按钮即可直接触发对应命令！

---

## 🏬 微软商店（Microsoft Store）上架要点

1. **`runFullTrust` 权限合规**：
   - 在 `Package.appxmanifest` 中声明了 `<rescap:Capability Name="runFullTrust" />`。
   - 提交商店审核时，在功能描述中注明应用为“用户自定义命令与脚本启动工具（类似 PowerToys / Windows Terminal）”，所有命令均由用户显式配置或取自本地公开开源预设，不存在静默下载未知二进制的行为。
2. **数据沙箱与隐私**：
   - 所有命令配置与运行日志存储在包的本地数据专属目录中，卸载时系统将彻底清除。
