<p align="center"><img src="assets/mouse-studio.png" width="112" height="112" alt="Mouse Studio 图标"></p>

# 指针工坊 · Mouse Studio

**简体中文** | [English](README.en.md)

一款轻量的 Windows 桌面鼠标定制工具。选择指针风格，拖动滑块调整大小与灵敏度，立即应用到真实的 Windows 系统鼠标。

**如果觉得这个小工具有用，欢迎给我一个 ⭐ Star！**

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
- **开机自启（可选）**：勾选后在当前用户登录 Windows 时启动到托盘，自动应用上次保存的样式、配色、大小与速度；取消勾选即可移除自启。
- **自动记忆配置**：调整成功后自动保存，手动重新打开也会恢复上次配置。“退出并恢复”只还原本次系统设置，保留偏好；点击“恢复原设置”则停止自动应用当前配置，直到再次调整。
- **自适应窗口**：卡片、分页按钮、预览和滑块随窗口大小重新布局，大窗口充分利用空间，小窗口支持纵向滚动。
- **托盘运行与恢复**：关闭窗口后继续生效；托盘右键“退出并恢复”或窗口内按 `Ctrl+Q` 重新加载 Windows 已保存的指针主题，并还原启动时的鼠标速度。恢复时不使用可能已异常的临时光标副本。

灵敏度控制的是 Windows 鼠标速度，不修改硬件 DPI。自行绘制指针或处理原始输入的软件可能不跟随系统设置。忙碌指针目前为静态圆环。首次使用新版请先选择一次指针配置；开机自启默认关闭，需要在窗口内勾选。请把程序保存在固定路径，Windows 若禁用了该启动项，需要在系统“启动应用”中恢复启用。

[查看大窗口布局](preview-1600x960.png)

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

应用图标包含 16–256 px 的九种尺寸，内嵌于 EXE，供资源管理器、桌面快捷方式、窗口和托盘使用。设计源文件位于 `assets/mouse-studio.svg`；运行 `.\build.ps1 -RebuildIcon` 可用 `tools/IconBuilder.cs` 重建配套 PNG / ICO 并编译。程序运行时无需携带图标文件。

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

另验证配置重启读回、登录式托盘启动、关闭前最后一次滑块值、取消自启、损坏配置处理，以及 800 / 1080 / 1600 宽度下的布局。测试使用独立临时配置目录和临时启动项，不开启用户的正式自启选项。登录启动流程通过独立进程模拟，未执行电脑重启。

`tools/ExitRegression.cs` 另在子进程结束后验证恢复效果，包括启动时指针已隐形、直接退出、恢复后退出、托盘退出、滑块待应用时退出、异常清理和重复恢复。检查系统的 14 类光标能否实际绘制，以及 `GetCursorInfo` 返回的当前显示光标是否可见。运行 `tools/test-exit.ps1` 可编译并执行此回归测试；测试会短暂改变鼠标设置，请先退出正在运行的程序。

## 实现

原生 C# / Windows Forms，通过 `CreateIconIndirect` 生成光标、`SetSystemCursor` 替换系统指针、`SystemParametersInfo` 读取和调整系统速度。不采集鼠标轨迹，不发送数据，不永久安装主题。

欢迎通过 Issues 提交问题或建议，通过 Pull Requests 贡献更多风格和改进。

## License

[MIT](LICENSE) © 2026 kaixuan2694
