# 指针工坊 · Mouse Studio

一款轻量的 Windows 桌面鼠标定制工具。选择指针风格，拖动滑块调整大小与灵敏度，立即应用到真实的 Windows 系统鼠标。

![指针工坊界面](preview.png)

![第二页：新增十款指针](preview-page2.png)

## 下载使用

从 [Releases](https://github.com/kaixuan2694/mouse-studio/releases/latest) 下载 `MouseStudio.exe`，双击运行；也可下载包含使用说明和恢复脚本的 `MouseStudio-Windows.zip`。

适用于 Windows 10 / 11，使用系统 .NET Framework 4.x。无需安装、管理员权限或联网。

## 功能

- **20 种原创风格，分两页展示**：第一页为素笺、藏锋、流光、桃夭、听竹、方寸、逐日、渡海、鎏月、星游；第二页为游龙、折纸、飞羽、长剑、团扇、小鱼、猫步、玉簪、山岚、火箭。
- **每款独立选色**：卡片右侧色块提供十种预设、自选颜色和恢复默认配色；自动记住各款配色，当前使用的指针即时更新。
- **指针大小**：24–96 px，滑动实时调整，保留正确的点击热点。
- **鼠标灵敏度**：调整 Windows 指针速度 1–20 档，保留原有鼠标加速选项。
- **14 类系统状态**：同步适配普通箭头、文字选择、链接手形、缩放、移动等状态。
- **托盘运行与恢复**：关闭窗口后继续生效；托盘右键“退出并恢复”或窗口内按 `Ctrl+Q` 还原启动时的指针与速度。

灵敏度控制的是 Windows 鼠标速度，不修改硬件 DPI。自行绘制指针或处理原始输入的软件可能不跟随系统设置。忙碌指针目前为静态圆环；不设置开机启动，注销或重启后需重新选择风格。

完整操作和异常恢复方式见 [使用说明](使用说明.md)。

## 从源码构建

在 Windows PowerShell 中执行：

```powershell
git clone https://github.com/kaixuan2694/mouse-studio.git
cd mouse-studio
.\build.ps1
.\MouseStudio.exe
```

构建脚本使用 Windows 自带的 .NET Framework C# 编译器，不依赖 NuGet 或其他第三方包。所有指针图形都由源码绘制，并内置在 EXE 中。

## 验证

```powershell
# 生成真实窗体预览，不应用鼠标设置
Start-Process .\MouseStudio.exe -ArgumentList '--preview' -Wait

# 实际修改系统鼠标并验证，结束后恢复
Start-Process .\MouseStudio.exe -ArgumentList '--self-test' -Wait
Get-Content .\test-results.txt
```

请先通过托盘退出已运行的程序，再运行验证命令。`--self-test` 会短暂改变当前电脑的鼠标设置，测试通过或发生异常时均尝试恢复。

验证覆盖 20 风格 × 3 尺寸 × 14 指针状态的实际系统句柄尺寸与热点，速度设置后的系统读回，恢复前后的位图、掩码与热点比对，以及翻页与选择保持、独立配色和系统指针颜色更新、两个滑块、恢复、托盘和退出操作。

## 实现

原生 C# / Windows Forms，通过 `CreateIconIndirect` 生成光标、`SetSystemCursor` 替换系统指针、`SystemParametersInfo` 读取和调整系统速度。不采集鼠标轨迹，不发送数据，不永久安装主题。

欢迎通过 Issues 提交问题或建议，通过 Pull Requests 贡献更多风格和改进。

## License

[MIT](LICENSE) © 2026 kaixuan2694
