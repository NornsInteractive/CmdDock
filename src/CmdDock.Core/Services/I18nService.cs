using CmdDock.Core.Models;
using System.Globalization;

namespace CmdDock.Core.Services;

public class I18nService
{
    private static readonly Lazy<I18nService> _instance = new(() => new I18nService());
    public static I18nService Instance => _instance.Value;

    public event Action? LanguageChanged;

    private string _currentConfigLanguage = AppSettingsService.LanguageSystem;

    public I18nService()
    {
        _currentConfigLanguage = AppSettingsService.GetLanguage();
    }

    public string CurrentLanguage
    {
        get => _currentConfigLanguage;
        set
        {
            if (_currentConfigLanguage != value)
            {
                _currentConfigLanguage = value;
                AppSettingsService.SetLanguage(value);
                LanguageChanged?.Invoke();
            }
        }
    }

    public string EffectiveLanguage
    {
        get
        {
            if (_currentConfigLanguage == AppSettingsService.LanguageChinese)
            {
                return AppSettingsService.LanguageChinese;
            }
            if (_currentConfigLanguage == AppSettingsService.LanguageEnglish)
            {
                return AppSettingsService.LanguageEnglish;
            }

            // Follow system: Check system UI culture
            var cultureName = CultureInfo.CurrentUICulture.Name;
            if (cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return AppSettingsService.LanguageChinese;
            }

            return AppSettingsService.LanguageEnglish;
        }
    }

    public string this[string key] => GetString(key);

    public string GetString(string key, string? fallback = null)
    {
        var lang = EffectiveLanguage;
        if (lang == AppSettingsService.LanguageChinese)
        {
            if (_zhStrings.TryGetValue(key, out var val))
            {
                return val;
            }
        }
        else
        {
            if (_enStrings.TryGetValue(key, out var val))
            {
                return val;
            }
        }

        return fallback ?? key;
    }

    public string Format(string key, params object[] args)
    {
        var template = GetString(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    public string TranslateCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return string.Empty;
        var key = $"Category.{category.Trim()}";
        return GetString(key, fallback: category);
    }

    public string TranslateShellType(ShellType shellType)
    {
        return shellType switch
        {
            ShellType.PowerShell => "PowerShell",
            ShellType.Cmd => GetString("ShellType.Cmd", "CMD (命令提示符)"),
            ShellType.Wsl => GetString("ShellType.Wsl", "WSL (Linux Bash)"),
            ShellType.Executable => GetString("ShellType.Executable", "可执行程序 (Exe)"),
            ShellType.UrlProtocol => GetString("ShellType.UrlProtocol", "URL 网址协议"),
            _ => shellType.ToString()
        };
    }

    public string TranslateExecutionMode(ExecutionMode mode)
    {
        return mode switch
        {
            ExecutionMode.Silent => GetString("ExecutionMode.Silent", "静默后台执行 (Silent)"),
            ExecutionMode.Terminal => GetString("ExecutionMode.Terminal", "弹出终端窗口 (Terminal)"),
            _ => mode.ToString()
        };
    }

    public string GetPresetLocalizedName(string id, string fallback)
    {
        var key = $"Preset.{id}.Name";
        return GetString(key, fallback: fallback);
    }

    public string GetPresetLocalizedDescription(string id, string fallback)
    {
        var key = $"Preset.{id}.Desc";
        return GetString(key, fallback: fallback);
    }

    private static readonly Dictionary<string, string> _zhStrings = new(StringComparer.OrdinalIgnoreCase)
    {
        // App Identity
        ["App.Title"] = "CmdDock - 快捷命令坞与桌面小组件",
        ["App.ShortTitle"] = "CmdDock",
        ["App.Tagline"] = "现代化 Windows 11 快捷命令停泊坞与桌面小组件",

        // Nav
        ["Nav.Commands"] = "快捷命令",
        ["Nav.Presets"] = "预设市场",
        ["Nav.Logs"] = "执行日志",
        ["Nav.Widgets"] = "Win11 小组件设置与指南",
        ["Nav.Settings"] = "设置",

        // Categories
        ["Category.全部"] = "全部",
        ["Category.常用"] = "常用",
        ["Category.系统"] = "系统",
        ["Category.网络"] = "网络",
        ["Category.开发"] = "开发",
        ["Category.运维"] = "运维",
        ["Category.工具"] = "工具",

        // ShellType & ExecutionMode
        ["ShellType.PowerShell"] = "PowerShell",
        ["ShellType.Cmd"] = "CMD (命令提示符)",
        ["ShellType.Wsl"] = "WSL (Linux Bash)",
        ["ShellType.Executable"] = "可执行程序 (Exe)",
        ["ShellType.UrlProtocol"] = "URL 网址协议",
        ["ExecutionMode.Silent"] = "静默后台执行 (Silent)",
        ["ExecutionMode.Terminal"] = "弹出终端窗口 (Terminal)",

        // Commands Panel
        ["Commands.New"] = "新建快捷命令",
        ["Commands.Refresh"] = "刷新",
        ["Commands.CategoryManage"] = "分类管理",
        ["Commands.AllCategories"] = "全部",
        ["Commands.CategoryFilterToolTip"] = "按分类筛选命令",
        ["Commands.CategoryManageToolTip"] = "管理快捷命令分类",
        ["Commands.EmptyTip"] = "尚未添加任何快捷命令",
        ["Commands.EmptySubtitle"] = "点击上方“新建快捷命令”或前往“预设市场”一键添加",
        ["Commands.Run"] = "执行",
        ["Commands.RunToolTip"] = "立即执行此命令",
        ["Commands.Edit"] = "编辑",
        ["Commands.EditToolTip"] = "编辑命令配置",
        ["Commands.Delete"] = "删除",
        ["Commands.DeleteToolTip"] = "删除此快捷命令",
        ["Commands.Pin"] = "置顶",
        ["Commands.Unpin"] = "取消置顶",
        ["Commands.Copy"] = "复制命令",
        ["Commands.Copied"] = "已复制命令到剪贴板",
        ["Commands.Executed"] = "命令已启动",
        ["Commands.DeleteConfirmTitle"] = "确认删除",
        ["Commands.DeleteConfirmContent"] = "确定要删除快捷命令 \"{0}\" 吗？此操作不可撤销。",
        ["Commands.Confirm"] = "确定",
        ["Commands.Cancel"] = "取消",
        ["Commands.StatusReady"] = "就绪",
        ["Commands.WidgetSwitchOn"] = "小组件",
        ["Commands.WidgetSwitchOff"] = "隐藏",

        // Presets Panel
        ["Presets.Title"] = "官方实用命令预设库",
        ["Presets.Subtitle"] = "精心整理的常用网络诊断、系统运维与开发高频命令，一键添加即可在 Win11 小组件中直接点击使用",
        ["Presets.Install"] = "添加至小组件与命令坞",
        ["Presets.Installed"] = "✓ 已添加至小组件",
        ["Presets.SearchPlaceholder"] = "搜索预设命令...",

        // Presets Specific Items (All 19 Built-in Presets)
        ["Preset.preset_restart_explorer.Name"] = "重启资源管理器",
        ["Preset.preset_restart_explorer.Desc"] = "重启 Windows Explorer 进程，解决任务栏冻结或桌面图标卡死",
        ["Preset.preset_clean_temp.Name"] = "清理系统临时文件",
        ["Preset.preset_clean_temp.Desc"] = "深度清空当前用户的临时垃圾与缓存目录，释放磁盘空间",
        ["Preset.preset_clear_clipboard.Name"] = "清空系统剪贴板",
        ["Preset.preset_clear_clipboard.Desc"] = "清理当前系统剪贴板中的敏感复制内容与图片数据",
        ["Preset.preset_lock_screen.Name"] = "一键锁屏",
        ["Preset.preset_lock_screen.Desc"] = "立即锁定当前 Windows 桌面工作会话",
        ["Preset.preset_taskmgr.Name"] = "快速打开任务管理器",
        ["Preset.preset_taskmgr.Desc"] = "即时唤起 Windows 任务管理器查看 CPU、内存与后台进程",
        ["Preset.preset_env_vars.Name"] = "系统环境变量设置",
        ["Preset.preset_env_vars.Desc"] = "直接调出 Windows 高级系统环境变量配置窗口",
        ["Preset.preset_disk_health.Name"] = "磁盘驱动器健康状态",
        ["Preset.preset_disk_health.Desc"] = "查询本机所有物理硬盘的健康与运行状态",
        ["Preset.preset_defender_scan.Name"] = "Windows 安全中心",
        ["Preset.preset_defender_scan.Desc"] = "快速打开 Windows Defender 安全中心防护面板",
        ["Preset.preset_flush_dns.Name"] = "刷新 DNS 缓存",
        ["Preset.preset_flush_dns.Desc"] = "清除本地 DNS 解析缓存，解决域名访问失效与解析异常",
        ["Preset.preset_port_listening.Name"] = "查看正在监听端口",
        ["Preset.preset_port_listening.Desc"] = "在终端中展示当前所有正在监听的 TCP 端口及所属进程 PID",
        ["Preset.preset_release_8080.Name"] = "快速释放 8080 端口",
        ["Preset.preset_release_8080.Desc"] = "查找并强制终止占用 8080 端口的后台进程",
        ["Preset.preset_public_ip.Name"] = "测试公网 IP 与连通性",
        ["Preset.preset_public_ip.Desc"] = "查询本机当前出口公网 IP 地址并测试网络延迟",
        ["Preset.preset_reset_winsock.Name"] = "重置 Winsock 协议栈",
        ["Preset.preset_reset_winsock.Desc"] = "重置系统网络套接字协议栈，解决严重断网或代理残留问题",
        ["Preset.preset_docker_ps.Name"] = "Docker 运行容器状态",
        ["Preset.preset_docker_ps.Desc"] = "查看正在运行的 Docker 容器列表、状态和端口映射",
        ["Preset.preset_docker_prune.Name"] = "清理 Docker 虚悬缓存",
        ["Preset.preset_docker_prune.Desc"] = "一键清理无用的 Docker 悬空镜像、容器和网络构建缓存",
        ["Preset.preset_python_env.Name"] = "Python 依赖包列表",
        ["Preset.preset_python_env.Desc"] = "列出当前 Python 环境下所有已安装的第三方包版本",
        ["Preset.preset_http_server.Name"] = "启动临时 HTTP 服务器",
        ["Preset.preset_http_server.Desc"] = "在 8080 端口快速启动一个本地静态网页服务器",
        ["Preset.preset_wsl_terminal.Name"] = "进入 WSL Linux 环境",
        ["Preset.preset_wsl_terminal.Desc"] = "一键打开并进入 Windows Subsystem for Linux 终端环境",
        ["Preset.preset_git_config.Name"] = "Git 全局配置查看",
        ["Preset.preset_git_config.Desc"] = "在终端中展示 Git 的全局配置项与来源文件",
        ["Preset.preset_ping_test.Name"] = "网络连通与延时测试",
        ["Preset.preset_ping_test.Desc"] = "向高可用公共 DNS 发起 Ping 延时测试，检测丢包率",
        ["Preset.preset_show_ip.Name"] = "查看内网与公网 IP",
        ["Preset.preset_show_ip.Desc"] = "显示本机局域网 IP 与出口公网 IP 地址信息",
        ["Preset.preset_active_ports.Name"] = "查看本地监听端口",
        ["Preset.preset_active_ports.Desc"] = "列出当前正在监听的所有 TCP 网络端口与占用进程 PID",
        ["Preset.preset_release_renew_ip.Name"] = "重置本机 IP 租约",
        ["Preset.preset_release_renew_ip.Desc"] = "释放并重新向路由器请求分配本地 DHCP IP 地址",
        ["Preset.preset_git_status.Name"] = "Git 仓库状态速查",
        ["Preset.preset_git_status.Desc"] = "快速显示当前工作目录的 Git 分支、暂存区与未提交变更",
        ["Preset.preset_node_version.Name"] = "Node & NPM 环境版本",
        ["Preset.preset_node_version.Desc"] = "速查当前系统配置的 Node.js 与包管理器运行版本",
        ["Preset.preset_kill_port.Name"] = "按端口查杀占用进程",
        ["Preset.preset_kill_port.Desc"] = "输入端口号，快速定位并安全终止占用端口的后台进程",
        ["Preset.preset_stop_wsl.Name"] = "彻底关闭 WSL 虚拟机",
        ["Preset.preset_stop_wsl.Desc"] = "完全终止 WSL 实例，立即释放其占用的宿主机内存",

        // Logs Panel
        ["Logs.Title"] = "命令执行历史日志",
        ["Logs.Subtitle"] = "记录最近 50 次命令触发的执行耗时、返回码及控制台输出信息",
        ["Logs.Clear"] = "清空日志",
        ["Logs.EmptyTip"] = "暂无执行日志记录",
        ["Logs.Success"] = "成功",
        ["Logs.Failed"] = "失败",
        ["Logs.Running"] = "运行中",
        ["Logs.Time"] = "时间",
        ["Logs.Duration"] = "耗时",
        ["Logs.ExitCode"] = "代码",
        ["Logs.ClearConfirmTitle"] = "清空日志确认",
        ["Logs.ClearConfirmContent"] = "确定要清空所有执行日志记录吗？",

        // Widget Settings & Guide Panel
        ["Widget.Title"] = "Windows 11 小组件接入指南与设置",
        ["Widget.Subtitle"] = "让快捷命令常驻任务栏左下角，支持多种个性化布局与无滚轮翻页方式",
        ["Widget.LayoutCardTitle"] = "小组件 UI 布局与翻页定制",
        ["Widget.LayoutCardDesc"] = "支持左侧菜单与下拉选择分类两种主布局结构，配合无滚轮翻页，修改后实时保存并自动推送到桌面小组件",
        ["Widget.ApplyBtn"] = "保存并应用到小组件",
        ["Widget.ApplyBtnSync"] = "立即同步小组件",
        ["Widget.LayoutHeader"] = "小组件主布局结构",
        ["Widget.LayoutSidebar"] = "左侧菜单模式 (SidebarRail - 侧栏导航)",
        ["Widget.LayoutDropdown"] = "下拉选择分类 (Dropdown - 顶部下拉框)",
        ["Widget.LayoutDescSidebar"] = "左侧导航模式：分类作为垂直导航栏置于左侧，点击分类实时切换右侧命令，视觉层级分明清晰。",
        ["Widget.LayoutDescDropdown"] = "下拉菜单模式：顶部紧凑下拉框快速切换分类，最大化卡片展示区域。",
        ["Widget.ViewModeHeader"] = "小组件展示形态",
        ["Widget.ViewGrid"] = "网格视图 (大图标 + 粗体双行卡片，适合触控与鼠标直击)",
        ["Widget.ViewList"] = "列表视图 (紧凑单行高密度，适合命令较多的高级运维用户)",
        ["Widget.PaginationHeader"] = "翻页控制器样式 (无滚轮适配)",
        ["Widget.PaginationInline"] = "方案 A: 标题栏极简内嵌翻页器 [◀ 1/3 ▶]",
        ["Widget.PaginationBottom"] = "方案 B: 底部专属控制微栏 [ 上一页 ◀ ] 第 1/3 页 [ ▶ 下一页 ]",
        ["Widget.PaginationDescInline"] = "标题栏内嵌微型翻页器：在顶栏右侧内嵌 [◀ 1/3 ▶]，零高度占用，极大节约 160px 视口高度，推荐首选。",
        ["Widget.PaginationDescBottom"] = "底部专属微栏：在卡片底部显示专用翻页按钮与页码状态（[ 上一页 ◀ ] 第 1/3 页 [ ▶ 下一页 ]）。",
        ["Widget.SmallConfigTitle"] = "小尺寸小组件 (Small) 专属按钮配置 (2行 × 2列，共 4 个)",
        ["Widget.SmallConfigDesc"] = "小尺寸小组件已彻底精简分类与顶部工具栏，纯净呈现 4 个快捷按钮。请在下方选择并排序要在小组件中显示的命令：",
        ["Widget.SmallOrderTip"] = "当前选定按钮与排列顺序 (第1-2个为第1行，第3-4个为第2行)：",
        ["Widget.SmallCountBadge"] = "已选 {0} / {1}",
        ["Widget.SmallAddHeader"] = "添加命令至小尺寸小组件",
        ["Widget.SmallAddBtn"] = "添加",
        ["Widget.SmallPlaceholder"] = "选择要添加的快捷命令...",
        ["Widget.StatusLayoutChanged"] = "小组件主布局已切换为：{0}",
        ["Widget.StatusPaginationChanged"] = "小组件翻页样式已切换为：{0}",
        ["Widget.StatusSynced"] = "✓ 已将最新布局与命令同步至 Win11 小组件！",
        ["Widget.PreviewTitle"] = "小组件形态预览 (Adaptive Cards)",
        ["Widget.PreviewSmallTitle"] = "小组件 (Small - 2行2列纯按钮)",
        ["Widget.PreviewMediumTitle"] = "中组件 (Medium - 5行2列)",
        ["Widget.PreviewManage"] = "⚙ 管理",
        ["Widget.GuideTitle"] = "如何添加到 Windows 11 左下角？",
        ["Widget.GuideStep1"] = "按下快捷键 Win + W 或点击任务栏左下角的小组件图标呼出面板。",
        ["Widget.GuideStep2"] = "点击小组件面板右上角的用户头像或 '+' (添加小组件) 按钮。",
        ["Widget.GuideStep3"] = "在列表中找到 CmdDock，点击右侧的固定 (Pin) 按钮将其钉选在面板上。",
        ["Widget.GuideStep4"] = "点击卡片右上角的三点菜单 (...) 可在 小 / 中 / 大 尺寸间自由切换。",
        ["Widget.AboutStoreReady"] = "Microsoft Store 认证就绪: 具备 runFullTrust 权限声明与隔离沙箱支持",
        ["Widget.ConfirmTitle"] = "⚠️ 执行二次确认",
        ["Widget.ConfirmPrompt"] = "该命令需要二次确认，是否立即在后台执行？",
        ["Widget.ConfirmExecute"] = "✓ 立即执行",
        ["Widget.ConfirmCancel"] = "✕ 取消",
        ["Widget.ConfirmCancelled"] = "已取消执行",
        ["Widget.ConfirmExecuting"] = "⏳ 正在执行: {0}",
        ["Widget.ExecSuccess"] = "✓ {0} 成功 ({1}ms)",
        ["Widget.ExecFailed"] = "✗ {0} 失败 (代码 {1})",

        // Settings Page
        ["Settings.Title"] = "设置",
        ["Settings.Subtitle"] = "个性化 CmdDock 的外观与偏好",
        ["Settings.Appearance"] = "外观与个性化",
        ["Settings.Theme"] = "应用主题",
        ["Settings.ThemeDesc"] = "选择 CmdDock 的界面主题模式，默认跟随 Windows 系统设置。",
        ["Settings.ThemeSystem"] = "跟随系统 (默认)",
        ["Settings.ThemeLight"] = "浅色",
        ["Settings.ThemeDark"] = "深色",
        ["Settings.Language"] = "显示语言",
        ["Settings.LanguageDesc"] = "选择 CmdDock 的界面显示语言，默认跟随 Windows 系统首选语言。",
        ["Settings.LangSystem"] = "跟随系统 (默认)",
        ["Settings.LangChinese"] = "简体中文",
        ["Settings.LangEnglish"] = "English",
        ["Settings.About"] = "关于 CmdDock",
        ["Settings.AboutDesc"] = "现代化 Windows 11 快捷命令停泊坞与桌面小组件",
        ["Settings.Version"] = "版本: 1.0.0.3 (Windows App SDK + .NET 8)",
        ["Settings.TechStack"] = "技术规范: Microsoft.Windows.Widgets.Providers / Adaptive Cards 1.5",
        ["Settings.DataFolder"] = "配置文件与数据目录",
        ["Settings.OpenDataFolder"] = "打开配置目录",
        ["Settings.PrivacyCardTitle"] = "隐私政策与数据安全声明",
        ["Settings.PrivacyCardSubtitle"] = "CmdDock 遵循纯本地运行安全原则，全量数据仅存放于您的计算机中。",
        ["Settings.PrivacyPoint1Title"] = "🛡️ 零数据收集",
        ["Settings.PrivacyPoint1Desc"] = "无需注册登录，不搜集用户姓名、邮箱、IP 地址、设备硬件标识或位置信息。",
        ["Settings.PrivacyPoint2Title"] = "💾 100% 本地存储",
        ["Settings.PrivacyPoint2Desc"] = "所有快捷命令、分类分组、小组件偏好与执行日志全量保存于本地 %LOCALAPPDATA% 目录下。",
        ["Settings.PrivacyPoint3Title"] = "🚫 无遥测与追踪",
        ["Settings.PrivacyPoint3Desc"] = "应用未集成任何第三方数据分析 SDK、广告追踪插件或后台遥测上报代码。",
        ["Settings.PrivacyPoint4Title"] = "⚡ 权限与命令受控",
        ["Settings.PrivacyPoint4Desc"] = "申请 runFullTrust 权限仅用于受控执行用户自定义命令，支持小组件卡片内二次确认。",
        ["Settings.PrivacyPoint5Title"] = "🗑️ 卸载彻底清除",
        ["Settings.PrivacyPoint5Desc"] = "通过 Windows 设置或微软商店卸载应用即可彻底清除本应用与所有本地数据。",
        ["Settings.PrivacyToggleExpand"] = "展开查看完整条款细则",
        ["Settings.PrivacyToggleCollapse"] = "收起完整条款细则",
        ["Privacy.FullText"] = @"【数据收集与使用】
• 不收集任何个人信息：CmdDock 无需注册账号或登录，不收集用户的姓名、电子邮件、IP 地址、设备硬件标识符、浏览历史或地理位置。
• 全量本地化存储：用户创建的所有快捷命令、分类分组、小组件偏好设置以及运行日志，全部保存在用户本地计算机的 %LOCALAPPDATA%\CmdDock 目录下，绝不上传到任何外部服务器。
• 无任何第三方遥测与追踪：本应用未集成任何数据分析 SDK、广告追踪插件或后台遥测上报代码。

【系统权限声明与命令执行】
• 完全信任权限（runFullTrust）：本应用申请该权限仅用于按用户的明确指令启动本地命令行解释器（如 PowerShell、CMD、WSL 等）执行自定义脚本。
• 用户完全受控：应用绝不会在后台静默执行任何未经用户触发的指令。对于关键系统操作，提供内嵌二次确认保护机制。

【网络访问】
本应用自身不设任何远程服务器，不主动发起任何网络通信。仅当用户自行配置的脚本中包含网络访问指令（例如 ping 或自定义 API 请求）时，才会产生相应的系统网络行为。

【数据删除】
所有数据均存储在您的本地计算机中。您可以通过 Windows“设置”或微软商店直接卸载本应用，系统将彻底清除本应用及其关联的所有本地数据。",

        // Command Edit Dialog
        ["Dialog.CommandEdit.NewTitle"] = "新建快捷命令",
        ["Dialog.CommandEdit.EditTitle"] = "编辑快捷命令",
        ["Dialog.CommandEdit.Save"] = "保存",
        ["Dialog.CommandEdit.Cancel"] = "取消",
        ["Dialog.CommandEdit.Name"] = "命令名称",
        ["Dialog.CommandEdit.NamePlaceholder"] = "例如: 刷新 DNS 缓存",
        ["Dialog.CommandEdit.Desc"] = "功能描述",
        ["Dialog.CommandEdit.DescPlaceholder"] = "简述命令用途",
        ["Dialog.CommandEdit.ShellType"] = "执行引擎",
        ["Dialog.CommandEdit.ExecMode"] = "执行模式",
        ["Dialog.CommandEdit.Category"] = "所属分类",
        ["Dialog.CommandEdit.CategoryPlaceholder"] = "选择分类",
        ["Dialog.CommandEdit.Script"] = "执行脚本 / 命令",
        ["Dialog.CommandEdit.ScriptPlaceholder"] = "例如: ipconfig /flushdns",
        ["Dialog.CommandEdit.Arguments"] = "命令附加参数 (可选)",
        ["Dialog.CommandEdit.ArgumentsPlaceholder"] = "传递给可执行文件的参数",
        ["Dialog.CommandEdit.WorkDir"] = "工作目录 (可选)",
        ["Dialog.CommandEdit.WorkDirPlaceholder"] = "留空则默认为当前目录",
        ["Dialog.CommandEdit.ShowInWidget"] = "在 Win11 小组件中展示",
        ["Dialog.CommandEdit.RequireConfirm"] = "执行前需要二次确认",
        ["Dialog.CommandEdit.WidgetAppearance"] = "小组件卡片样式自定义",
        ["Dialog.CommandEdit.BgMode"] = "背景模式",
        ["Dialog.CommandEdit.BgTransparent"] = "默认透明",
        ["Dialog.CommandEdit.BgCustom"] = "自定义背景颜色",
        ["Dialog.CommandEdit.BgColor"] = "卡片背景颜色",
        ["Dialog.CommandEdit.BgColorPlaceholder"] = "例如: #2563EB",
        ["Dialog.CommandEdit.ChooseIcon"] = "选择图标",
        ["Dialog.CommandEdit.PickPicture"] = "图片",
        ["Dialog.CommandEdit.PickPictureToolTip"] = "选择本地图片文件 (.png, .jpg, .ico, .svg)",
        ["Dialog.CommandEdit.BackToForm"] = "返回表单",

        // Category Management Dialog
        ["Dialog.Category.Title"] = "分类管理",
        ["Dialog.Category.Done"] = "完成",
        ["Dialog.Category.Desc"] = "管理命令分类与图标。修改或删除分类时，将自动同步更新关联的快捷命令和小组件。",
        ["Dialog.Category.Placeholder"] = "输入新分类名称...",
        ["Dialog.Category.Add"] = "添加分类",
        ["Dialog.Category.MoveUp"] = "上移",
        ["Dialog.Category.MoveDown"] = "下移",
        ["Dialog.Category.Edit"] = "编辑分类（修改名称与图标）",
        ["Dialog.Category.Delete"] = "删除分类",
        ["Dialog.Category.BackToList"] = "返回列表",
        ["Dialog.Category.EditTitle"] = "编辑分类",
        ["Dialog.Category.Name"] = "分类名称",
        ["Dialog.Category.Icon"] = "分类图标",
        ["Dialog.Category.CurrentIcon"] = "当前图标",
        ["Dialog.Category.ChangeIconHint"] = "点击右侧按钮更换系统官方图标或本地图片",
        ["Dialog.Category.SaveEdit"] = "保存修改",
        ["Dialog.Category.DeleteConfirmTitle"] = "删除分类",
        ["Dialog.Category.DeleteConfirmContent"] = "删除分类 \"{0}\" 后，该分类下的命令将被移至默认分类。确定继续吗？",
        ["Dialog.Category.ChangeIconToolTip"] = "点击直接更换分类图标",
        ["Dialog.Category.StatusHint"] = "共 {0} 个分类，可在新建命令或小组件中直接筛选",
        ["Dialog.Category.StatusExists"] = "分类「{0}」已存在",
        ["Dialog.Category.StatusDeleteMin"] = "至少保留一个分类，无法删除",
        ["Dialog.Category.StatusLoadFailed"] = "加载分类失败: {0}",
        ["Dialog.Category.StatusImportFailed"] = "导入图片失败: {0}",
        ["Dialog.Category.EditCategoryTitle"] = "编辑分类「{0}」",
        ["Dialog.Category.ChangeIconFor"] = "更换分类「{0}」图标",
        ["Dialog.Category.NewCategoryIconToolTip"] = "为新分类选择图标",
        ["Dialog.Category.EditNamePlaceholder"] = "输入分类名称...",

        // Icon Picker Sub-Panel
        ["IconPicker.SelectSystemIcon"] = "选择系统图标",
        ["IconPicker.SelectCategoryIcon"] = "选择分类图标",
        ["IconPicker.SearchPlaceholder"] = "输入关键词模糊搜索 (如: 终端, cmd, 网络, wifi, 设置, git, 锁屏...)",
        ["IconPicker.All"] = "全部",
        ["IconPicker.Operations"] = "常用与操作",
        ["IconPicker.Hardware"] = "系统与硬件",
        ["IconPicker.Network"] = "网络通信",
        ["IconPicker.DevOps"] = "开发与运维",
        ["IconPicker.LocalImage"] = "本地图片",
        ["IconPicker.OfficialIcon"] = "官方系统图标",
        ["IconPicker.WindowsNative"] = "系统原生图标 (Segoe Fluent Icons)",
        ["IconPicker.CustomLocalImage"] = "自定义本地图片",
        ["IconPicker.LocalImageToolTip"] = "从本地文件选择 PNG/JPG/ICO 图片",
        ["IconPicker.CountTotal"] = "共 {0} 款",
        ["IconPicker.CountMatched"] = "匹配到 {0} 款",
        ["IconPicker.HintSelect"] = "💡 点击任意图标卡片即可直接应用并返回",
        ["IconPicker.HintEmpty"] = "未找到匹配的图标，建议尝试其它搜索词或切换分类",
        ["IconPicker.Back"] = "返回",

        // Execution Confirm & Widget Details
        ["Dialog.RunConfirm.Title"] = "执行确认",
        ["Dialog.RunConfirm.Content"] = "即将执行命令: \"{0}\"\n\n脚本内容: {1}\n\n是否确认执行？",
        ["Dialog.RunConfirm.Execute"] = "立即执行",
        ["Widget.SmallMaxTip"] = "小尺寸小组件最多容纳 4 个快捷按钮，请先移除其他按钮。",
        ["Widget.SmallAdded"] = "已添加「{0}」至小组件专属按钮。",
        ["Widget.SmallRemoved"] = "已从小组件专属按钮中移除「{0}」。",
        ["Mockup.FlushDns"] = "刷新 DNS",
        ["Mockup.RestartExplorer"] = "重启桌面",
        ["Mockup.ClearClipboard"] = "清空剪贴板",
        ["Mockup.ClearClipboardShort"] = "清剪贴板",
        ["Mockup.ViewPorts"] = "查看端口",
        ["Mockup.LockScreen"] = "一键锁屏",
        ["Mockup.PingTest"] = "测试延时",
        ["Mockup.GitStatus"] = "Git 状态",
        ["Mockup.CleanTemp"] = "清理垃圾",
        ["Mockup.IpDetails"] = "IP 详情",
        ["Mockup.NodeVersion"] = "Node 版本"
    };

    private static readonly Dictionary<string, string> _enStrings = new(StringComparer.OrdinalIgnoreCase)
    {
        // App Identity
        ["App.Title"] = "CmdDock - Command Dock & Desktop Widget",
        ["App.ShortTitle"] = "CmdDock",
        ["App.Tagline"] = "Modern Windows 11 Command Dock & Desktop Widget",

        // Nav
        ["Nav.Commands"] = "Commands",
        ["Nav.Presets"] = "Presets Market",
        ["Nav.Logs"] = "Execution Logs",
        ["Nav.Widgets"] = "Widget Settings & Guide",
        ["Nav.Settings"] = "Settings",

        // Categories
        ["Category.全部"] = "All",
        ["Category.常用"] = "Common",
        ["Category.系统"] = "System",
        ["Category.网络"] = "Network",
        ["Category.开发"] = "Development",
        ["Category.运维"] = "DevOps",
        ["Category.工具"] = "Tools",

        // ShellType & ExecutionMode
        ["ShellType.PowerShell"] = "PowerShell",
        ["ShellType.Cmd"] = "CMD (Command Prompt)",
        ["ShellType.Wsl"] = "WSL (Linux Bash)",
        ["ShellType.Executable"] = "Executable (.exe)",
        ["ShellType.UrlProtocol"] = "URL Protocol",
        ["ExecutionMode.Silent"] = "Silent Background",
        ["ExecutionMode.Terminal"] = "Terminal Window",

        // Commands Panel
        ["Commands.New"] = "New Command",
        ["Commands.Refresh"] = "Refresh",
        ["Commands.CategoryManage"] = "Categories",
        ["Commands.AllCategories"] = "All",
        ["Commands.CategoryFilterToolTip"] = "Filter commands by category",
        ["Commands.CategoryManageToolTip"] = "Manage command categories",
        ["Commands.EmptyTip"] = "No commands yet",
        ["Commands.EmptySubtitle"] = "Click \"New Command\" above or install from \"Presets Market\"",
        ["Commands.Run"] = "Run",
        ["Commands.RunToolTip"] = "Execute this command immediately",
        ["Commands.Edit"] = "Edit",
        ["Commands.EditToolTip"] = "Edit command configuration",
        ["Commands.Delete"] = "Delete",
        ["Commands.DeleteToolTip"] = "Delete this command",
        ["Commands.Pin"] = "Pin",
        ["Commands.Unpin"] = "Unpin",
        ["Commands.Copy"] = "Copy Command",
        ["Commands.Copied"] = "Command copied to clipboard",
        ["Commands.Executed"] = "Command launched",
        ["Commands.DeleteConfirmTitle"] = "Confirm Delete",
        ["Commands.DeleteConfirmContent"] = "Are you sure you want to delete command \"{0}\"? This cannot be undone.",
        ["Commands.Confirm"] = "Confirm",
        ["Commands.Cancel"] = "Cancel",
        ["Commands.StatusReady"] = "Ready",
        ["Commands.WidgetSwitchOn"] = "Widget",
        ["Commands.WidgetSwitchOff"] = "Hidden",

        // Presets Panel
        ["Presets.Title"] = "Productivity Command Presets",
        ["Presets.Subtitle"] = "Curated network diagnostics, system management, and development scripts ready to install with one click",
        ["Presets.Install"] = "Add to Widget & Dock",
        ["Presets.Installed"] = "✓ Added to Widget",
        ["Presets.SearchPlaceholder"] = "Search preset commands...",

        // Presets Specific Items (All 19 Built-in Presets)
        ["Preset.preset_restart_explorer.Name"] = "Restart Windows Explorer",
        ["Preset.preset_restart_explorer.Desc"] = "Restart Windows Explorer to resolve frozen taskbar or desktop icons",
        ["Preset.preset_clean_temp.Name"] = "Clean Temp Files",
        ["Preset.preset_clean_temp.Desc"] = "Deep clean current user temporary cache to reclaim disk storage",
        ["Preset.preset_clear_clipboard.Name"] = "Clear Clipboard",
        ["Preset.preset_clear_clipboard.Desc"] = "Clear Windows clipboard contents to protect sensitive data",
        ["Preset.preset_lock_screen.Name"] = "Lock Screen",
        ["Preset.preset_lock_screen.Desc"] = "Instantly lock current Windows desktop session",
        ["Preset.preset_taskmgr.Name"] = "Open Task Manager",
        ["Preset.preset_taskmgr.Desc"] = "Quickly launch Windows Task Manager to monitor processes and CPU",
        ["Preset.preset_env_vars.Name"] = "Environment Variables",
        ["Preset.preset_env_vars.Desc"] = "Open Windows Advanced System Environment Variables dialog",
        ["Preset.preset_disk_health.Name"] = "Disk Health Check",
        ["Preset.preset_disk_health.Desc"] = "Inspect local physical drive health status and free storage capacity",
        ["Preset.preset_defender_scan.Name"] = "Windows Security",
        ["Preset.preset_defender_scan.Desc"] = "Quickly open Windows Defender Security Center dashboard",
        ["Preset.preset_flush_dns.Name"] = "Flush DNS Cache",
        ["Preset.preset_flush_dns.Desc"] = "Flush local DNS resolver cache to fix domain resolution issues",
        ["Preset.preset_port_listening.Name"] = "View Listening Ports",
        ["Preset.preset_port_listening.Desc"] = "List active listening TCP ports and associated process PIDs",
        ["Preset.preset_release_8080.Name"] = "Release Port 8080",
        ["Preset.preset_release_8080.Desc"] = "Find and terminate background processes occupying port 8080",
        ["Preset.preset_public_ip.Name"] = "Check Public IP & Ping",
        ["Preset.preset_public_ip.Desc"] = "Query outbound public IP address and test network latency",
        ["Preset.preset_reset_winsock.Name"] = "Reset Winsock Stack",
        ["Preset.preset_reset_winsock.Desc"] = "Reset Windows socket catalog to fix network connectivity or proxy issues",
        ["Preset.preset_docker_ps.Name"] = "Docker Containers",
        ["Preset.preset_docker_ps.Desc"] = "List all currently running Docker containers",
        ["Preset.preset_docker_prune.Name"] = "Prune Docker Cache",
        ["Preset.preset_docker_prune.Desc"] = "Clean unused dangling Docker images, containers, and build cache",
        ["Preset.preset_python_env.Name"] = "Python Pip Packages",
        ["Preset.preset_python_env.Desc"] = "List all installed Python package versions in current environment",
        ["Preset.preset_http_server.Name"] = "Start Local HTTP Server",
        ["Preset.preset_http_server.Desc"] = "Quickly launch local static HTTP web server on port 8080",
        ["Preset.preset_wsl_terminal.Name"] = "Launch WSL Terminal",
        ["Preset.preset_wsl_terminal.Desc"] = "Open Windows Subsystem for Linux (WSL) interactive shell",
        ["Preset.preset_git_config.Name"] = "View Git Configuration",
        ["Preset.preset_git_config.Desc"] = "Display Git global configuration and origin files in terminal",
        ["Preset.preset_ping_test.Name"] = "Network Ping Test",
        ["Preset.preset_ping_test.Desc"] = "Ping public DNS servers to test network latency and packet loss",
        ["Preset.preset_show_ip.Name"] = "Show IP Addresses",
        ["Preset.preset_show_ip.Desc"] = "Display local LAN IP and public outbound IP address",
        ["Preset.preset_active_ports.Name"] = "View Listening Ports",
        ["Preset.preset_active_ports.Desc"] = "List active listening TCP ports and associated process PIDs",
        ["Preset.preset_release_renew_ip.Name"] = "Release & Renew IP",
        ["Preset.preset_release_renew_ip.Desc"] = "Release and renew DHCP IP configuration from router",
        ["Preset.preset_git_status.Name"] = "Git Repo Status",
        ["Preset.preset_git_status.Desc"] = "Inspect current Git branch, staging area, and uncommitted changes",
        ["Preset.preset_node_version.Name"] = "Node & NPM Versions",
        ["Preset.preset_node_version.Desc"] = "Quickly check installed Node.js and npm runtime versions",
        ["Preset.preset_kill_port.Name"] = "Kill Process by Port",
        ["Preset.preset_kill_port.Desc"] = "Locate and terminate background process occupying a specific port",
        ["Preset.preset_stop_wsl.Name"] = "Shutdown WSL",
        ["Preset.preset_stop_wsl.Desc"] = "Terminate WSL instances to instantly reclaim host RAM",

        // Logs Panel
        ["Logs.Title"] = "Execution History Logs",
        ["Logs.Subtitle"] = "Recent 50 executions showing duration, exit code, and console output",
        ["Logs.Clear"] = "Clear Logs",
        ["Logs.EmptyTip"] = "No execution history logs yet",
        ["Logs.Success"] = "Success",
        ["Logs.Failed"] = "Failed",
        ["Logs.Running"] = "Running",
        ["Logs.Time"] = "Time",
        ["Logs.Duration"] = "Duration",
        ["Logs.ExitCode"] = "Code",
        ["Logs.ClearConfirmTitle"] = "Confirm Clear Logs",
        ["Logs.ClearConfirmContent"] = "Are you sure you want to clear all execution history logs?",

        // Widget Settings & Guide Panel
        ["Widget.Title"] = "Windows 11 Widget Settings & Guide",
        ["Widget.Subtitle"] = "Dock quick commands directly into Windows 11 Widgets board with personalized layout",
        ["Widget.LayoutCardTitle"] = "Widget UI Layout & Pagination Customization",
        ["Widget.LayoutCardDesc"] = "Choose between left sidebar and dropdown layout with scroll-free pagination, automatically synced to widget board",
        ["Widget.ApplyBtn"] = "Save & Apply to Widget",
        ["Widget.ApplyBtnSync"] = "Sync to Widget Now",
        ["Widget.LayoutHeader"] = "Widget Main Layout",
        ["Widget.LayoutSidebar"] = "Left Sidebar Tabs (SidebarRail - Side Navigation)",
        ["Widget.LayoutDropdown"] = "Dropdown Category (Dropdown - Top Compact Picker)",
        ["Widget.LayoutDescSidebar"] = "Sidebar Mode: Categories displayed as a vertical rail on the left with commands on the right.",
        ["Widget.LayoutDescDropdown"] = "Dropdown Mode: Compact category picker on top to maximize command card area.",
        ["Widget.ViewModeHeader"] = "Widget Display Form",
        ["Widget.ViewGrid"] = "Grid View (Large icons + bold card, ideal for direct clicks)",
        ["Widget.ViewList"] = "List View (Compact single-line, high density for power users)",
        ["Widget.PaginationHeader"] = "Pagination Controller Style (Scroll-free)",
        ["Widget.PaginationInline"] = "Option A: Minimalist Header Inline Pager [◀ 1/3 ▶]",
        ["Widget.PaginationBottom"] = "Option B: Dedicated Bottom Mini-Bar [ Prev ◀ ] Page 1/3 [ ▶ Next ]",
        ["Widget.PaginationDescInline"] = "Inline Pager: Embedded [◀ 1/3 ▶] on the top right, saves vertical height, highly recommended.",
        ["Widget.PaginationDescBottom"] = "Dedicated Bottom Bar: Shows navigation buttons and status at the bottom ([ Prev ◀ ] Page 1/3 [ ▶ Next ]).",
        ["Widget.SmallConfigTitle"] = "Small Widget (2×2 4 Items) Customization",
        ["Widget.SmallConfigDesc"] = "Small widget displays 4 top commands. Add and configure order here:",
        ["Widget.SmallOrderTip"] = "Selected buttons and display order (1-2: row 1, 3-4: row 2):",
        ["Widget.SmallCountBadge"] = "Selected {0} / {1}",
        ["Widget.SmallAddHeader"] = "Add Command to Small Widget",
        ["Widget.SmallAddBtn"] = "Add",
        ["Widget.SmallPlaceholder"] = "Select quick command to add to small widget...",
        ["Widget.StatusLayoutChanged"] = "Widget layout switched to: {0}",
        ["Widget.StatusPaginationChanged"] = "Widget pagination style switched to: {0}",
        ["Widget.StatusSynced"] = "✓ Latest layout and commands synced to Windows 11 Widget!",
        ["Widget.PreviewTitle"] = "Widget Preview (Adaptive Cards)",
        ["Widget.PreviewSmallTitle"] = "Small Widget (2×2 Pure Buttons)",
        ["Widget.PreviewMediumTitle"] = "Medium Widget (5×2 Grid)",
        ["Widget.PreviewManage"] = "⚙ Manage",
        ["Widget.GuideTitle"] = "How to add to Windows 11 Widgets Board?",
        ["Widget.GuideStep1"] = "Press Win + W or click the Widgets icon in the bottom-left taskbar.",
        ["Widget.GuideStep2"] = "Click your avatar or '+' (Add Widgets) at the top right of the panel.",
        ["Widget.GuideStep3"] = "Find CmdDock in the list and click Pin to anchor it to your board.",
        ["Widget.GuideStep4"] = "Click the three dots (...) on the card to switch between Small / Medium / Large.",
        ["Widget.AboutStoreReady"] = "Microsoft Store Ready: runFullTrust capability and isolated sandbox support",
        ["Widget.ConfirmTitle"] = "⚠️ Confirm Execution",
        ["Widget.ConfirmPrompt"] = "This command requires confirmation. Execute now?",
        ["Widget.ConfirmExecute"] = "✓ Confirm & Run",
        ["Widget.ConfirmCancel"] = "✕ Cancel",
        ["Widget.ConfirmCancelled"] = "Execution cancelled",
        ["Widget.ConfirmExecuting"] = "⏳ Executing: {0}",
        ["Widget.ExecSuccess"] = "✓ {0} Success ({1}ms)",
        ["Widget.ExecFailed"] = "✗ {0} Failed (Code {1})",

        // Settings Page
        ["Settings.Title"] = "Settings",
        ["Settings.Subtitle"] = "Personalize CmdDock appearance and preferences",
        ["Settings.Appearance"] = "Appearance & Personalization",
        ["Settings.Theme"] = "App Theme",
        ["Settings.ThemeDesc"] = "Select CmdDock's theme mode. Follows Windows system setting by default.",
        ["Settings.ThemeSystem"] = "Follow System (Default)",
        ["Settings.ThemeLight"] = "Light",
        ["Settings.ThemeDark"] = "Dark",
        ["Settings.Language"] = "Display Language",
        ["Settings.LanguageDesc"] = "Select CmdDock's interface language. Follows Windows preferred language by default.",
        ["Settings.LangSystem"] = "Follow System (Default)",
        ["Settings.LangChinese"] = "简体中文 (Simplified Chinese)",
        ["Settings.LangEnglish"] = "English",
        ["Settings.About"] = "About CmdDock",
        ["Settings.AboutDesc"] = "Modern Windows 11 Command Dock & Desktop Widget",
        ["Settings.Version"] = "Version: 1.0.0.3 (Windows App SDK + .NET 8)",
        ["Settings.TechStack"] = "Technology: Microsoft.Windows.Widgets.Providers / Adaptive Cards 1.5",
        ["Settings.DataFolder"] = "Configuration & Data Directory",
        ["Settings.OpenDataFolder"] = "Open Data Folder",
        ["Settings.PrivacyCardTitle"] = "Privacy Policy & Data Security",
        ["Settings.PrivacyCardSubtitle"] = "CmdDock operates strictly on your local device with zero telemetry and zero data collection.",
        ["Settings.PrivacyPoint1Title"] = "🛡️ Zero Data Collection",
        ["Settings.PrivacyPoint1Desc"] = "No account or login required. We do not collect names, emails, IP addresses, or device IDs.",
        ["Settings.PrivacyPoint2Title"] = "💾 100% Local Storage",
        ["Settings.PrivacyPoint2Desc"] = "All commands, categories, widget settings, and logs reside strictly in %LOCALAPPDATA%.",
        ["Settings.PrivacyPoint3Title"] = "🚫 Zero Telemetry & Tracking",
        ["Settings.PrivacyPoint3Desc"] = "Contains zero third-party analytics SDKs, advertising beacons, or telemetry probes.",
        ["Settings.PrivacyPoint4Title"] = "⚡ Controlled Execution",
        ["Settings.PrivacyPoint4Desc"] = "runFullTrust is used strictly to spawn user-specified terminal scripts with secondary confirmation.",
        ["Settings.PrivacyPoint5Title"] = "🗑️ Clean Removal",
        ["Settings.PrivacyPoint5Desc"] = "Uninstalling via Windows Settings or Microsoft Store completely removes all local app data.",
        ["Settings.PrivacyToggleExpand"] = "Expand Full Legal Policy",
        ["Settings.PrivacyToggleCollapse"] = "Collapse Full Legal Policy",
        ["Privacy.FullText"] = @"[Data Collection and Usage]
• No Personal Information Collected: CmdDock requires no registration or login. We do not collect names, emails, IP addresses, device identifiers, browsing history, or location data.
• 100% Local Storage: All commands, categories, widget configurations, and execution logs created by users are stored strictly on your local device under %LOCALAPPDATA%\CmdDock, and never uploaded to any external servers.
• No Telemetry: The application contains zero advertising SDKs, tracking beacons, or analytics probes.

[System Permissions and Execution]
• Full Trust Capability (runFullTrust): CmdDock requires the runFullTrust capability solely to spawn the user-specified command interpreters (PowerShell, CMD, WSL bash) as explicitly triggered by the user.
• User Discretion: No command runs automatically without user interaction. Critical actions are protected by in-widget secondary confirmation.

[Network Access]
The Application does not host any remote servers or initiate unsolicited network connections. Network activity occurs only if a user-configured script specifically contains network commands (such as ping or custom API requests).

[Data Deletion]
Uninstalling CmdDock via Windows Settings or Microsoft Store completely removes the application and all associated local data files.",

        // Command Edit Dialog
        ["Dialog.CommandEdit.NewTitle"] = "New Quick Command",
        ["Dialog.CommandEdit.EditTitle"] = "Edit Quick Command",
        ["Dialog.CommandEdit.Save"] = "Save",
        ["Dialog.CommandEdit.Cancel"] = "Cancel",
        ["Dialog.CommandEdit.Name"] = "Command Name",
        ["Dialog.CommandEdit.NamePlaceholder"] = "e.g. Flush DNS Cache",
        ["Dialog.CommandEdit.Desc"] = "Description",
        ["Dialog.CommandEdit.DescPlaceholder"] = "Briefly describe command purpose",
        ["Dialog.CommandEdit.ShellType"] = "Shell Engine",
        ["Dialog.CommandEdit.ExecMode"] = "Execution Mode",
        ["Dialog.CommandEdit.Category"] = "Category",
        ["Dialog.CommandEdit.CategoryPlaceholder"] = "Select category",
        ["Dialog.CommandEdit.Script"] = "Script / Command",
        ["Dialog.CommandEdit.ScriptPlaceholder"] = "e.g. ipconfig /flushdns",
        ["Dialog.CommandEdit.Arguments"] = "Additional Arguments (Optional)",
        ["Dialog.CommandEdit.ArgumentsPlaceholder"] = "Arguments passed to executable",
        ["Dialog.CommandEdit.WorkDir"] = "Working Directory (Optional)",
        ["Dialog.CommandEdit.WorkDirPlaceholder"] = "Leave empty for current directory",
        ["Dialog.CommandEdit.ShowInWidget"] = "Show in Win11 Widget",
        ["Dialog.CommandEdit.RequireConfirm"] = "Require confirmation before executing",
        ["Dialog.CommandEdit.WidgetAppearance"] = "Widget Card Style Customization",
        ["Dialog.CommandEdit.BgMode"] = "Background Mode",
        ["Dialog.CommandEdit.BgTransparent"] = "Default Transparent",
        ["Dialog.CommandEdit.BgCustom"] = "Custom Background Color",
        ["Dialog.CommandEdit.BgColor"] = "Card Background Color",
        ["Dialog.CommandEdit.BgColorPlaceholder"] = "e.g. #2563EB",
        ["Dialog.CommandEdit.ChooseIcon"] = "Choose Icon",
        ["Dialog.CommandEdit.PickPicture"] = "Image",
        ["Dialog.CommandEdit.PickPictureToolTip"] = "Choose local image file (.png, .jpg, .ico, .svg)",
        ["Dialog.CommandEdit.BackToForm"] = "Back to Form",

        // Category Management Dialog
        ["Dialog.Category.Title"] = "Category Management",
        ["Dialog.Category.Done"] = "Done",
        ["Dialog.Category.Desc"] = "Manage command categories and icons. Changes will automatically sync with commands and widgets.",
        ["Dialog.Category.Placeholder"] = "Enter new category name...",
        ["Dialog.Category.Add"] = "Add Category",
        ["Dialog.Category.MoveUp"] = "Move Up",
        ["Dialog.Category.MoveDown"] = "Move Down",
        ["Dialog.Category.Edit"] = "Edit Category",
        ["Dialog.Category.Delete"] = "Delete Category",
        ["Dialog.Category.BackToList"] = "Back to List",
        ["Dialog.Category.EditTitle"] = "Edit Category",
        ["Dialog.Category.Name"] = "Category Name",
        ["Dialog.Category.Icon"] = "Category Icon",
        ["Dialog.Category.CurrentIcon"] = "Current Icon",
        ["Dialog.Category.ChangeIconHint"] = "Click button to select system icon or pick local image",
        ["Dialog.Category.SaveEdit"] = "Save Changes",
        ["Dialog.Category.DeleteConfirmTitle"] = "Delete Category",
        ["Dialog.Category.DeleteConfirmContent"] = "After deleting category \"{0}\", its commands will be moved to the default category. Continue?",
        ["Dialog.Category.ChangeIconToolTip"] = "Click to change category icon",
        ["Dialog.Category.StatusHint"] = "{0} categories total, available in command filters and widgets",
        ["Dialog.Category.StatusExists"] = "Category \"{0}\" already exists",
        ["Dialog.Category.StatusDeleteMin"] = "Cannot delete the only remaining category",
        ["Dialog.Category.StatusLoadFailed"] = "Failed to load categories: {0}",
        ["Dialog.Category.StatusImportFailed"] = "Failed to import image: {0}",
        ["Dialog.Category.EditCategoryTitle"] = "Edit Category \"{0}\"",
        ["Dialog.Category.ChangeIconFor"] = "Change icon for \"{0}\"",
        ["Dialog.Category.NewCategoryIconToolTip"] = "Select icon for new category",
        ["Dialog.Category.EditNamePlaceholder"] = "Enter category name...",

        // Icon Picker Sub-Panel
        ["IconPicker.SelectSystemIcon"] = "Select System Icon",
        ["IconPicker.SelectCategoryIcon"] = "Select Category Icon",
        ["IconPicker.SearchPlaceholder"] = "Search icons (e.g. terminal, cmd, network, wifi, git, lock...)",
        ["IconPicker.All"] = "All",
        ["IconPicker.Operations"] = "Operations",
        ["IconPicker.Hardware"] = "System & Hardware",
        ["IconPicker.Network"] = "Network",
        ["IconPicker.DevOps"] = "Dev & Ops",
        ["IconPicker.LocalImage"] = "Local Image",
        ["IconPicker.OfficialIcon"] = "Official System Icon",
        ["IconPicker.WindowsNative"] = "Native Fluent Icon (Segoe Fluent Icons)",
        ["IconPicker.CustomLocalImage"] = "Custom Local Image",
        ["IconPicker.LocalImageToolTip"] = "Choose PNG/JPG/ICO from local files",
        ["IconPicker.CountTotal"] = "{0} icons total",
        ["IconPicker.CountMatched"] = "{0} icons matched",
        ["IconPicker.HintSelect"] = "💡 Click any icon card to select and return",
        ["IconPicker.HintEmpty"] = "No matching icons found. Try different keywords or categories.",
        ["IconPicker.Back"] = "Back",

        // Execution Confirm & Widget Details
        ["Dialog.RunConfirm.Title"] = "Confirm Execution",
        ["Dialog.RunConfirm.Content"] = "About to execute command: \"{0}\"\n\nScript: {1}\n\nConfirm execution?",
        ["Dialog.RunConfirm.Execute"] = "Execute Now",
        ["Widget.SmallMaxTip"] = "Small widget can hold up to 4 quick buttons. Please remove one first.",
        ["Widget.SmallAdded"] = "Added \"{0}\" to small widget buttons.",
        ["Widget.SmallRemoved"] = "Removed \"{0}\" from small widget buttons.",
        ["Mockup.FlushDns"] = "Flush DNS",
        ["Mockup.RestartExplorer"] = "Restart Desktop",
        ["Mockup.ClearClipboard"] = "Clear Clipboard",
        ["Mockup.ClearClipboardShort"] = "Clear Clip",
        ["Mockup.ViewPorts"] = "View Ports",
        ["Mockup.LockScreen"] = "Lock Screen",
        ["Mockup.PingTest"] = "Ping Test",
        ["Mockup.GitStatus"] = "Git Status",
        ["Mockup.CleanTemp"] = "Clean Temp",
        ["Mockup.IpDetails"] = "IP Details",
        ["Mockup.NodeVersion"] = "Node Version"
    };
}
