# TranslateTool

自动翻译桌面工具 — 支持百度、Google、微软、DeepL 多引擎翻译。

## 功能特性

- **四种翻译模式**：文本粘贴翻译、文件翻译（.txt/.docx/.pdf）、屏幕框选翻译（OCR）、剪贴板监听自动翻译
- **多引擎支持**：百度翻译（默认）、Google 翻译、微软翻译、DeepL，支持多引擎同时对比
- **悬浮窗设计**：无边框可拖动窗口，支持置顶、边缘拖拽调整大小
- **OCR 文字识别**：基于 Tesseract 引擎，支持中英文
- **翻译历史**：本地保存最近 100 条记录，支持搜索和复制
- **翻译缓存**：7 天有效期，避免重复请求
- **多语言界面**：支持中文/英文切换
- **深色模式**：参考 Windows 11 Mica 设计语言
- **全局快捷键**：Ctrl+Shift+T 快速呼出悬浮窗
- **语音朗读**：Windows SAPI 朗读原文/译文

## 技术栈

- C# / .NET 8 WPF + HandyControl + CommunityToolkit.Mvvm
- MVVM 架构 + 依赖注入 (Microsoft.Extensions.DependencyInjection)
- 翻译引擎：百度翻译 API / Google Translate / Bing Translate / DeepL API
- OCR：Tesseract 5.2.0

## 运行方式

```powershell
# 构建
dotnet build src/TranslateTool.csproj --configuration Release

# 运行
dotnet run --project src/TranslateTool.csproj
```

## 项目结构

```
src/
├── Models/          — 数据模型
├── Services/        — 翻译引擎、OCR、剪贴板等服务
├── ViewModels/      — MVVM ViewModel 层
├── Views/           — WPF 窗口界面
├── Themes/          — 浅色/深色主题
├── Converters/      — 值转换器
├── Localization/    — 中英文多语言资源
└── Utils/           — 工具类
```

## 翻译引擎配置

| 引擎 | 申请地址 | 免费额度 |
|------|----------|----------|
| 百度翻译 | https://fanyi-api.baidu.com/product/11 | 200 万字符/月 |
| DeepL | https://www.deepl.com/pro-api | 50 万字符/月 |

## 打包发布

使用 Inno Setup 打包，脚本位于 `installer.iss`。

```powershell
# 需要先安装 Inno Setup，然后运行：
iscc installer.iss
```

## License

MIT"# TranslateTool"  
