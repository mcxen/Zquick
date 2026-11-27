# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-11-27

### Changed
- 目标框架切换至 Windows 原生内置的 **.NET Framework 4.8 (`net48`)**，去除庞大的 .NET 8 独立运行库打包，单文件可执行程序缩减至 ~150 KB，安装包瘦身至 **~1 MB**，Windows 10/11 零依赖开箱即用。
- 重写 JSON 配置与历史记录序列化层为系统内置 `JavaScriptSerializer`，保持零外部 NuGet 依赖。

### Fixed
- 嵌入 `app.manifest` 启用现代 Windows Common Controls v6 视觉样式，并在程序入口调用 `SetProcessDpiAwarenessContext(PerMonitorV2)`，彻底修复高分辨率/高 DPI（150%–250% 缩放）下界面文字裁切、顶栏按钮遮挡及表格行高与复选框挤压问题。
- 所有窗体按钮与表格列头/行高改为按系统 DPI 缩放并启用 `AutoSize`，保证各缩放比例下清晰完整显示。

## [1.0.0] - 2026-11-27

### Added
- 首个正式版本发布（**Zquick**），支持无管理员权限注册 Explorer 二级级联右键菜单。
- 内置 8 类可扩展动作：目录结构概览、文件夹加前缀、按文件夹名重命名子文件、递归清理前缀、7z/ZIP 批量压缩（含加密及安全移入回收站）、FFmpeg 无损视频旋转、自定义脚本以及组合流水线。
- 提供重命名变更预览确认窗口与本地历史记录（`history.json`）一键逆序撤销功能。
- 提供 Inno Setup Windows 安装包、便携版及 GitHub Actions 自动构建发布流程。
