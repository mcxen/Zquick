# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-27

### Added
- 首个正式版本发布（**Zquick**，基于 .NET 8 WinForms 构建，零外部 NuGet 依赖）。
- 支持无管理员权限写入 `HKCU` 注册表，在 Windows 资源管理器文件夹及空白处右键提供二级级联菜单。
- 内置 8 类可扩展动作：
  - 目录结构概览与目录树导出复制
  - 子文件夹批量加前缀（支持历史前缀记忆）
  - 按当前文件夹名 / 各自子文件夹名重命名子文件（按扩展名分组、补零、单文件可免编号、幂等跳过）
  - 递归清理指定前缀列表
  - 批量 7z / ZIP 压缩（支持外层目录保留/平铺、AES-256 加密 + DPAPI 加密存密、校验退出码后安全移入回收站）
  - FFmpeg 批量无损旋转视频（90° / 180° / 270°，自动创建输出目录，支持 Metadata 与 DisplayRotation 模式）
  - 外挂自定义 `.bat` / `.ps1` / `.exe` 脚本
  - 组合流水线（自由串联多个动作按序执行，任意步取消即安全中止）
- 提供重命名变更预览确认窗口与本地历史记录（`history.json`）一键逆序撤销功能。
- 提供 Inno Setup Windows 安装包（支持安装时自动注册右键菜单、卸载时自动清理注册表）、独立单文件便携版及 GitHub Actions 自动构建发布流程。
