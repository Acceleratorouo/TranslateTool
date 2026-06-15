---
title: 自动翻译软件设计文档
date: 2026-06-14
status: approved
---

# 自动翻译软件设计文档

## 概述

一个 Windows 桌面翻译工具，类似 DeepL 客户端。通过系统托盘悬浮窗入口，右键选择四种翻译模式（文本粘贴翻译、文件翻译、全屏自动翻译、截图框选翻译），支持谷歌/微软免费网页翻译及自定义 API 接口。

## 技术栈

| 维度 | 技术 |
|------|------|
| 语言 | C# / .NET 8 |
| UI 框架 | WPF + HandyControl |
| 模式 | MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |
| 托盘 | Hardcodet.NotifyIcon.Wpf |
| 屏幕截取 | System.Drawing.Graphics.CopyFromScreen |
| OCR | Microsoft.ML.OnnxRuntime 或 Tesseract |
| 文件解析 | DocumentFormat.OpenXml (.docx), PdfPig (.pdf) |
| 快捷键 | Win32 RegisterHotKey P/Invoke |

## 核心组件

### 1. 悬浮窗 (FloatingWindow)
- 无边框、置顶、可拖动（长按左键拖动）
- 右键弹出 ContextMenu 选择翻译模式
- 显示翻译结果

### 2. 翻译引擎 (TranslationEngine)
- `ITranslator` 统一接口
- `GoogleTranslator` — 谷歌免费网页翻译
- `MicrosoftTranslator` — 微软免费网页翻译
- `ApiTranslator` — 自定义 API 接口
- `TranslatorFactory` — 根据配置创建引擎实例

### 3. 翻译模式 (TranslationMode)
- `Paste` — 文本粘贴翻译（剪贴板内容）
- `File` — 文件翻译（拖入 .docx/.pdf/.txt）
- `FullScreen` — 全屏自动翻译（截图→OCR→翻译）
- `Region` — 截图框选翻译（半透明遮罩层框选区域→OCR→翻译）

### 4. 辅助服务
- `ScreenCaptureService` — 屏幕截取
- `OcrService` — 文字识别
- `ClipboardWatcher` — 剪贴板监听
- `HotkeyService` — 全局快捷键注册

## 项目结构

```
TranslateTool/
├── src/
│   ├── Models/           — 数据模型
│   ├── Services/         — 业务逻辑
│   ├── ViewModels/       — MVVM 视图模型
│   ├── Views/            — XAML 视图
│   ├── Converters/       — 值转换器
│   └── Utils/            — 原生方法封装
├── TranslateTool.csproj
└── docs/
```

## 数据流

用户操作 → 模式路由 → 文本获取 → 翻译执行 → 结果展示

## 语言支持

中/英/日/韩/法/德/西/俄等主流语言
