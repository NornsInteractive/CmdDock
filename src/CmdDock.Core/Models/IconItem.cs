namespace CmdDock.Core.Models;

public class IconItem
{
    public string Glyph { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;

    public string DisplayTitle => $"{Name} ({EnglishName})";

    public string LocalizedName => CmdDock.Core.Services.I18nService.Instance.EffectiveLanguage == "en-US" ? EnglishName : Name;

    public string LocalizedCategory => CmdDock.Core.Services.I18nService.Instance.EffectiveLanguage == "en-US" ? (Category switch
    {
        "常用与操作" => "Operations",
        "系统与硬件" => "System & Hardware",
        "网络通信" => "Network",
        "开发与运维" => "Dev & Ops",
        _ => Category
    }) : Category;

    public static readonly IReadOnlyList<IconItem> AllIcons = new List<IconItem>
    {
        // ==================== 常用与操作 (Operations) ====================
        new()
        {
            Glyph = "\uE756",
            Name = "终端控制台",
            EnglishName = "Terminal",
            Category = "常用与操作",
            Keywords = "cmd powershell bash sh zsh shell 终端 控制台 命令行 命令 cli console terminal dos prompt"
        },
        new()
        {
            Glyph = "\uE749",
            Name = "快速闪电",
            EnglishName = "Lightning / Quick",
            Category = "常用与操作",
            Keywords = "闪电 快速 极速 立即 run quick fast lightning speed power"
        },
        new()
        {
            Glyph = "\uE895",
            Name = "启动火箭",
            EnglishName = "Rocket / Launch",
            Category = "常用与操作",
            Keywords = "火箭 启动 发射 部署 run launch rocket start send"
        },
        new()
        {
            Glyph = "\uE768",
            Name = "启动运行",
            EnglishName = "Play / Run",
            Category = "常用与操作",
            Keywords = "运行 播放 启动 开始 执行 play run start execute"
        },
        new()
        {
            Glyph = "\uE769",
            Name = "暂停挂起",
            EnglishName = "Pause",
            Category = "常用与操作",
            Keywords = "暂停 挂起 等待 pause wait suspend"
        },
        new()
        {
            Glyph = "\uE71A",
            Name = "停止中止",
            EnglishName = "Stop",
            Category = "常用与操作",
            Keywords = "停止 中止 杀死 结束 kill stop terminate cancel end"
        },
        new()
        {
            Glyph = "\uE777",
            Name = "重启同步",
            EnglishName = "Restart / Sync",
            Category = "常用与操作",
            Keywords = "重启 同步 刷新 restart sync reload reboot"
        },
        new()
        {
            Glyph = "\uE72C",
            Name = "刷新页面",
            EnglishName = "Refresh",
            Category = "常用与操作",
            Keywords = "刷新 缓存 清除 reload refresh f5 update"
        },
        new()
        {
            Glyph = "\uE713",
            Name = "系统设置",
            EnglishName = "Settings",
            Category = "常用与操作",
            Keywords = "设置 配置 选项 偏好 setting settings config options gear preferences"
        },
        new()
        {
            Glyph = "\uE74D",
            Name = "清理垃圾",
            EnglishName = "Clean / Delete",
            Category = "常用与操作",
            Keywords = "清理 删除 垃圾 卸载 移除 临时 clean delete remove trash clear purge temp"
        },
        new()
        {
            Glyph = "\uE72E",
            Name = "锁定屏幕",
            EnglishName = "Lock",
            Category = "常用与操作",
            Keywords = "锁屏 锁定 安全 密码 屏幕 lock workstation screen secure"
        },
        new()
        {
            Glyph = "\uE785",
            Name = "解锁开放",
            EnglishName = "Unlock",
            Category = "常用与操作",
            Keywords = "解锁 开放 权限 提权 unlock access open permission"
        },
        new()
        {
            Glyph = "\uE72B",
            Name = "安全盾牌",
            EnglishName = "Shield / Security",
            Category = "常用与操作",
            Keywords = "安全 防护 盾牌 杀毒 defender security shield protect antivirus"
        },
        new()
        {
            Glyph = "\uE7BA",
            Name = "警告提示",
            EnglishName = "Warning",
            Category = "常用与操作",
            Keywords = "警告 警示 注意 危险 warning alert caution danger"
        },
        new()
        {
            Glyph = "\uE783",
            Name = "错误阻止",
            EnglishName = "Error / Cancel",
            Category = "常用与操作",
            Keywords = "错误 失败 阻止 异常 取消 error fail cross cancel block"
        },
        new()
        {
            Glyph = "\uE73E",
            Name = "完成成功",
            EnglishName = "CheckMark",
            Category = "常用与操作",
            Keywords = "对勾 成功 完成 通过 check checkmark success done pass ok"
        },
        new()
        {
            Glyph = "\uE735",
            Name = "收藏星标",
            EnglishName = "Star / Favorite",
            Category = "常用与操作",
            Keywords = "收藏 星标 常用 标记 star favorite bookmark like"
        },
        new()
        {
            Glyph = "\uE710",
            Name = "新建添加",
            EnglishName = "Add / New",
            Category = "常用与操作",
            Keywords = "新建 添加 增加 创建 add new plus create"
        },
        new()
        {
            Glyph = "\uE711",
            Name = "关闭退出",
            EnglishName = "Close / Exit",
            Category = "常用与操作",
            Keywords = "关闭 退出 结束 close exit dismiss quit"
        },
        new()
        {
            Glyph = "\uE77F",
            Name = "系统剪贴板",
            EnglishName = "Clipboard",
            Category = "常用与操作",
            Keywords = "剪贴板 粘贴 复制 clipboard paste copy board"
        },
        new()
        {
            Glyph = "\uE8C8",
            Name = "复制副本",
            EnglishName = "Copy",
            Category = "常用与操作",
            Keywords = "复制 拷贝 副本 copy duplicate clone"
        },
        new()
        {
            Glyph = "\uE8D2",
            Name = "剪切内容",
            EnglishName = "Cut",
            Category = "常用与操作",
            Keywords = "剪切 剪刀 cut scissor"
        },
        new()
        {
            Glyph = "\uE74E",
            Name = "保存文件",
            EnglishName = "Save",
            Category = "常用与操作",
            Keywords = "保存 存盘 写入 save write disk"
        },
        new()
        {
            Glyph = "\uE70F",
            Name = "编辑修改",
            EnglishName = "Edit",
            Category = "常用与操作",
            Keywords = "编辑 修改 铅笔 更改 edit modify pencil change update"
        },
        new()
        {
            Glyph = "\uE7A6",
            Name = "撤销操作",
            EnglishName = "Undo",
            Category = "常用与操作",
            Keywords = "撤销 回退 上一步 undo revert back"
        },
        new()
        {
            Glyph = "\uE8A1",
            Name = "重做操作",
            EnglishName = "Redo",
            Category = "常用与操作",
            Keywords = "重做 前进 下一步 redo forward repeat"
        },
        new()
        {
            Glyph = "\uE72D",
            Name = "共享分享",
            EnglishName = "Share",
            Category = "常用与操作",
            Keywords = "分享 共享 发送 share transmit export"
        },

        // ==================== 系统与硬件 (Hardware) ====================
        new()
        {
            Glyph = "\uE7F8",
            Name = "电脑主机",
            EnglishName = "PC / Desktop",
            Category = "系统与硬件",
            Keywords = "电脑 主机 桌面 台式机 pc desktop computer machine hardware"
        },
        new()
        {
            Glyph = "\uE7EE",
            Name = "笔记本电脑",
            EnglishName = "Laptop",
            Category = "系统与硬件",
            Keywords = "笔记本 笔电 便携 laptop notebook portable"
        },
        new()
        {
            Glyph = "\uE8EA",
            Name = "平板设备",
            EnglishName = "Tablet",
            Category = "系统与硬件",
            Keywords = "平板 触屏 surface tablet pad touch"
        },
        new()
        {
            Glyph = "\uE717",
            Name = "智能手机",
            EnglishName = "Phone / Mobile",
            Category = "系统与硬件",
            Keywords = "手机 移动 电话 phone mobile android iphone"
        },
        new()
        {
            Glyph = "\uE82D",
            Name = "处理器性能",
            EnglishName = "CPU / Chip",
            Category = "系统与硬件",
            Keywords = "cpu 处理器 芯片 性能 算力 processor chip hardware core"
        },
        new()
        {
            Glyph = "\uEDA2",
            Name = "磁盘驱动器",
            EnglishName = "HardDrive / Disk",
            Category = "系统与硬件",
            Keywords = "硬盘 磁盘 存储 空间 c盘 d盘 ssd hdd disk drive storage harddrive"
        },
        new()
        {
            Glyph = "\uEBB5",
            Name = "电池健康",
            EnglishName = "Battery",
            Category = "系统与硬件",
            Keywords = "电池 电量 电源 续航 battery charge power energy"
        },
        new()
        {
            Glyph = "\uE7E8",
            Name = "电源插头",
            EnglishName = "PowerPlug / Power",
            Category = "系统与硬件",
            Keywords = "电源 插头 供电 适配器 power plug adapter energy"
        },
        new()
        {
            Glyph = "\uE9D9",
            Name = "任务管理器",
            EnglishName = "TaskView / TaskMgr",
            Category = "系统与硬件",
            Keywords = "任务 监控 进程 管理器 task taskmgr taskview monitor process"
        },
        new()
        {
            Glyph = "\uE765",
            Name = "键盘操作",
            EnglishName = "Keyboard",
            Category = "系统与硬件",
            Keywords = "键盘 按键 快捷键 输入 宏 keyboard key hotkey input typing"
        },
        new()
        {
            Glyph = "\uE962",
            Name = "鼠标设备",
            EnglishName = "Mouse",
            Category = "系统与硬件",
            Keywords = "鼠标 点击 光标 mouse click pointer cursor"
        },
        new()
        {
            Glyph = "\uE8B9",
            Name = "音频声音",
            EnglishName = "Volume / Audio",
            Category = "系统与硬件",
            Keywords = "声音 音量 音频 扬声器 喇叭 volume audio sound speaker"
        },
        new()
        {
            Glyph = "\uE767",
            Name = "音量静音",
            EnglishName = "Volume Mute",
            Category = "系统与硬件",
            Keywords = "静音 关闭声音 喇叭 mute silent quiet"
        },
        new()
        {
            Glyph = "\uE76C",
            Name = "麦克风录音",
            EnglishName = "Microphone",
            Category = "系统与硬件",
            Keywords = "麦克风 录音 语音 话筒 mic microphone record audio voice"
        },
        new()
        {
            Glyph = "\uE720",
            Name = "耳机音响",
            EnglishName = "Headphone",
            Category = "系统与硬件",
            Keywords = "耳机 耳麦 监听 headphone headset audio"
        },
        new()
        {
            Glyph = "\uE790",
            Name = "摄像头视频",
            EnglishName = "Camera",
            Category = "系统与硬件",
            Keywords = "相机 摄像 视频 抓拍 镜头 camera video webcam photo"
        },
        new()
        {
            Glyph = "\uE82F",
            Name = "打印机设备",
            EnglishName = "Printer",
            Category = "系统与硬件",
            Keywords = "打印 打印机 出纸 复印 print printer document paper"
        },
        new()
        {
            Glyph = "\uE7FC",
            Name = "游戏手柄",
            EnglishName = "GameController",
            Category = "系统与硬件",
            Keywords = "游戏 手柄 控制器 摇杆 xbox game controller gamepad joystick"
        },

        // ==================== 网络通信 (Network) ====================
        new()
        {
            Glyph = "\uE774",
            Name = "互联网 DNS",
            EnglishName = "Globe / DNS",
            Category = "网络通信",
            Keywords = "互联网 网络 dns 域名 球 地球 globe internet web network browser ip url"
        },
        new()
        {
            Glyph = "\uE8B0",
            Name = "端口网线连接",
            EnglishName = "Cable / Connect",
            Category = "网络通信",
            Keywords = "网线 端口 连接 线路 网口 cable connect ethernet wire port link"
        },
        new()
        {
            Glyph = "\uE706",
            Name = "无线 WiFi",
            EnglishName = "WiFi / Network",
            Category = "网络通信",
            Keywords = "wifi 无线 网络 信号 宽带 路由 wlan wifi wireless network signal"
        },
        new()
        {
            Glyph = "\uE704",
            Name = "以太局域网",
            EnglishName = "Ethernet / LAN",
            Category = "网络通信",
            Keywords = "以太网 局域网 网卡 lan ethernet network local"
        },
        new()
        {
            Glyph = "\uE7B5",
            Name = "蓝牙通信",
            EnglishName = "Bluetooth",
            Category = "网络通信",
            Keywords = "蓝牙 配对 无线 bluetooth wireless pair connect"
        },
        new()
        {
            Glyph = "\uE721",
            Name = "搜索路由探测",
            EnglishName = "Search / Ping",
            Category = "网络通信",
            Keywords = "搜索 探测 ping 寻址 路由 查找 放大镜 search ping find locate lookup"
        },
        new()
        {
            Glyph = "\uE74C",
            Name = "云端服务",
            EnglishName = "Cloud",
            Category = "网络通信",
            Keywords = "云 云端 云盘 服务器 同步 网盘 cloud sync remote drive"
        },
        new()
        {
            Glyph = "\uE896",
            Name = "下载拉取",
            EnglishName = "Download",
            Category = "网络通信",
            Keywords = "下载 拉取 获取 保存 download pull fetch get receive"
        },
        new()
        {
            Glyph = "\uE898",
            Name = "上传推送",
            EnglishName = "Upload",
            Category = "网络通信",
            Keywords = "上传 推送 发布 提交 upload push publish commit post"
        },
        new()
        {
            Glyph = "\uE71B",
            Name = "网址链接",
            EnglishName = "Link / URL",
            Category = "网络通信",
            Keywords = "链接 网址 超链接 协议 url link href hyperlink web"
        },
        new()
        {
            Glyph = "\uE8A7",
            Name = "后台服务器",
            EnglishName = "Server",
            Category = "网络通信",
            Keywords = "服务器 节点 主机 集群 后台 server host node cluster backend"
        },
        new()
        {
            Glyph = "\uE8B7",
            Name = "文件资源目录",
            EnglishName = "Folder",
            Category = "网络通信",
            Keywords = "文件夹 目录 资源 路径 folder dir directory path explorer"
        },
        new()
        {
            Glyph = "\uE838",
            Name = "打开浏览目录",
            EnglishName = "FolderOpen",
            Category = "网络通信",
            Keywords = "打开 浏览 文件夹 目录 资源 open folder browse explore"
        },
        new()
        {
            Glyph = "\uE8F4",
            Name = "新建文件夹",
            EnglishName = "NewFolder",
            Category = "网络通信",
            Keywords = "新建 文件夹 分类 归档 new folder create mkdir"
        },
        new()
        {
            Glyph = "\uE7C3",
            Name = "脚本配置文件",
            EnglishName = "Document / Script",
            Category = "网络通信",
            Keywords = "文件 脚本 配置 文本 代码 文档 document script file text config yaml json txt"
        },
        new()
        {
            Glyph = "\uE7C5",
            Name = "运行历史日志",
            EnglishName = "Note / Log",
            Category = "网络通信",
            Keywords = "日志 记录 笔记 历史 note log record history audit"
        },
        new()
        {
            Glyph = "\uE83F",
            Name = "压缩归档包",
            EnglishName = "Zip / Archive",
            Category = "网络通信",
            Keywords = "压缩 解压 zip rar 7z tar 归档 archive compress package"
        },
        new()
        {
            Glyph = "\uE89C",
            Name = "安全证书",
            EnglishName = "Certificate",
            Category = "网络通信",
            Keywords = "证书 凭证 ssl https 签名 授权 certificate cert auth ssl sign"
        },

        // ==================== 开发与运维 (DevOps) ====================
        new()
        {
            Glyph = "\uE943",
            Name = "代码开发工具",
            EnglishName = "Code",
            Category = "开发与运维",
            Keywords = "代码 编程 开发 ide vscode visual studio code program develop dev"
        },
        new()
        {
            Glyph = "\uE9CE",
            Name = "脚本控制台",
            EnglishName = "PowerShell / Console",
            Category = "开发与运维",
            Keywords = "powershell 脚本 控制台 命令行 pwsh console script terminal"
        },
        new()
        {
            Glyph = "\uE90F",
            Name = "系统维护工具",
            EnglishName = "Repair / Tools",
            Category = "开发与运维",
            Keywords = "工具 修复 扳手 螺丝刀 维护 tools repair maintenance fix"
        },
        new()
        {
            Glyph = "\uE9F9",
            Name = "开发者工具包",
            EnglishName = "DevTools",
            Category = "开发与运维",
            Keywords = "开发者 开发包 调试 工具箱 devtools developer toolkit dev"
        },
        new()
        {
            Glyph = "\uE968",
            Name = "调试诊断分析",
            EnglishName = "Bug / Debug",
            Category = "开发与运维",
            Keywords = "bug 调试 报错 诊断 排查 断点 debug issue defect fix"
        },
        new()
        {
            Glyph = "\uE9E9",
            Name = "Git 版本分支",
            EnglishName = "Branch / Git",
            Category = "开发与运维",
            Keywords = "git 分支 版本 合并 代码库 branch merge version vcs repository"
        },
        new()
        {
            Glyph = "\uE7F4",
            Name = "软件包与容器",
            EnglishName = "Package / Docker",
            Category = "开发与运维",
            Keywords = "包 软件包 依赖 镜像 容器 docker package container npm pip cargo"
        },
        new()
        {
            Glyph = "\uE71D",
            Name = "安全密钥凭据",
            EnglishName = "Key / Credential",
            Category = "开发与运维",
            Keywords = "密钥 密码 钥匙 凭据 token ssh secret key password credential token"
        },
        new()
        {
            Glyph = "\uEA86",
            Name = "系统健康诊断",
            EnglishName = "Diagnostic / Health",
            Category = "开发与运维",
            Keywords = "体检 诊断 心跳 健康 监控 health diagnostic heartbeat status"
        },
        new()
        {
            Glyph = "\uE9D5",
            Name = "注册表配置项",
            EnglishName = "Registry / Config",
            Category = "开发与运维",
            Keywords = "注册表 系统项 配置 环境 regedit registry setting config"
        },
        new()
        {
            Glyph = "\uE8A9",
            Name = "视图网格布局",
            EnglishName = "ViewAll / Grid",
            Category = "开发与运维",
            Keywords = "网格 全览 矩阵 视图 布局 grid viewall matrix layout"
        },
        new()
        {
            Glyph = "\uE8C0",
            Name = "紧凑列表布局",
            EnglishName = "List",
            Category = "开发与运维",
            Keywords = "列表 清单 排版 视图 list view menu lines"
        },
        new()
        {
            Glyph = "\uE724",
            Name = "执行历史时钟",
            EnglishName = "History / Clock",
            Category = "开发与运维",
            Keywords = "历史 时钟 时间 记录 计时 history clock time timeline"
        },
        new()
        {
            Glyph = "\uE916",
            Name = "秒表耗时统计",
            EnglishName = "Timer / Stopwatch",
            Category = "开发与运维",
            Keywords = "秒表 耗时 计时器 测速 stopwatch timer benchmark duration"
        },
        new()
        {
            Glyph = "\uE718",
            Name = "定时任务提醒",
            EnglishName = "Alarm / Schedule",
            Category = "开发与运维",
            Keywords = "闹钟 定时 计划 提醒 schedule cron alarm reminder cronjob"
        },
        new()
        {
            Glyph = "\uE8C5",
            Name = "计算器换算",
            EnglishName = "Calculator",
            Category = "开发与运维",
            Keywords = "计算 计算器 换算 统计 calculator calc math count"
        },
        new()
        {
            Glyph = "\uE823",
            Name = "日历计划周期",
            EnglishName = "Calendar",
            Category = "开发与运维",
            Keywords = "日历 日期 计划 周 月 calendar date schedule day"
        },
        new()
        {
            Glyph = "\uE738",
            Name = "标记关注项",
            EnglishName = "Flag",
            Category = "开发与运维",
            Keywords = "旗帜 标记 重点 关注 目标 flag mark target focus"
        },
        new()
        {
            Glyph = "\uE730",
            Name = "固定置顶快捷",
            EnglishName = "Pin",
            Category = "开发与运维",
            Keywords = "置顶 固定 图钉 快捷 pin top bookmark"
        },
        new()
        {
            Glyph = "\uE840",
            Name = "标签分类标记",
            EnglishName = "Tag",
            Category = "开发与运维",
            Keywords = "标签 标记 分类 分组 tag label badge category"
        },
        new()
        {
            Glyph = "\uE719",
            Name = "详细属性信息",
            EnglishName = "Info",
            Category = "开发与运维",
            Keywords = "信息 属性 详情 提示 info information detail about"
        },
        new()
        {
            Glyph = "\uE787",
            Name = "帮助手册说明",
            EnglishName = "Help / Question",
            Category = "开发与运维",
            Keywords = "帮助 问号 手册 文档 疑问 help question faq doc"
        },
        new()
        {
            Glyph = "\uE80F",
            Name = "首页主页导航",
            EnglishName = "Home",
            Category = "开发与运维",
            Keywords = "主页 首页 根目录 房子 home index start"
        },
        new()
        {
            Glyph = "\uE789",
            Name = "个人用户身份",
            EnglishName = "User / Account",
            Category = "开发与运维",
            Keywords = "用户 账号 个人 身份 头像 user account profile person"
        },
        new()
        {
            Glyph = "\uE77B",
            Name = "用户组与权限",
            EnglishName = "Group / Users",
            Category = "开发与运维",
            Keywords = "用户组 团队 成员 权限 group users team members"
        },
        new()
        {
            Glyph = "\uE7BE",
            Name = "筛选过滤条件",
            EnglishName = "Filter",
            Category = "开发与运维",
            Keywords = "过滤 筛选 漏斗 filter query search funnel"
        },
        new()
        {
            Glyph = "\uE8CB",
            Name = "排序先后顺序",
            EnglishName = "Sort",
            Category = "开发与运维",
            Keywords = "排序 升序 降序 顺序 sort order rank priority"
        }
    };

    public static IReadOnlyList<IconItem> Filter(string? query, string? category = "全部")
    {
        var list = AllIcons.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category) &&
            category != "全部" &&
            category != "All" &&
            !string.Equals(category, CmdDock.Core.Services.I18nService.Instance["IconPicker.All"], StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(i =>
                string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.LocalizedCategory, category, StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(category, "Operations", StringComparison.OrdinalIgnoreCase) && i.Category == "常用与操作") ||
                (string.Equals(category, "System & Hardware", StringComparison.OrdinalIgnoreCase) && i.Category == "系统与硬件") ||
                (string.Equals(category, "Network", StringComparison.OrdinalIgnoreCase) && i.Category == "网络通信") ||
                (string.Equals(category, "Dev & Ops", StringComparison.OrdinalIgnoreCase) && i.Category == "开发与运维"));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return list.ToList();
        }

        var q = query.Trim().ToLowerInvariant();
        return list.Where(i => MatchesFuzzy(i, q)).ToList();
    }

    public static IconItem? FindByGlyph(string? glyph)
    {
        if (string.IsNullOrWhiteSpace(glyph)) return null;
        return AllIcons.FirstOrDefault(i => string.Equals(i.Glyph, glyph, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesFuzzy(IconItem item, string query)
    {
        // 1. Direct contains match on Name, EnglishName, Category or Keywords
        if (item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.EnglishName.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Category.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Keywords.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;

        // 2. Acronym / subsequence match (e.g. "pwsh" matches "PowerShell", "cmd" matches "Command")
        if (IsSubsequence(query, item.Name.ToLowerInvariant()) ||
            IsSubsequence(query, item.EnglishName.ToLowerInvariant()) ||
            IsSubsequence(query, item.Keywords.ToLowerInvariant()))
        {
            return true;
        }

        return false;
    }

    private static bool IsSubsequence(string pattern, string text)
    {
        int p = 0;
        for (int t = 0; t < text.Length && p < pattern.Length; t++)
        {
            if (char.ToLowerInvariant(text[t]) == pattern[p])
            {
                p++;
            }
        }
        return p == pattern.Length;
    }
}
