# TranslateTool

自动翻译桌面工具 — 支持百度、Google、微软、DeepL 及 AI (LLM) 多引擎翻译。

## 功能特性

- **四种翻译模式**：文本粘贴翻译、文件翻译（.txt/.docx/.pdf）、屏幕框选翻译（OCR）、剪贴板监听自动翻译
- **多引擎支持**：百度翻译（默认）、Google 翻译、微软翻译、DeepL、AI 翻译（LLM），支持多引擎同时对比
- **AI 翻译**：基于 LLM 的智能翻译，支持 Ollama（本地 Gemma 4 E4B）、OpenAI、OpenRouter、DeepSeek、SiliconFlow 等供应商，可自定义系统提示词和参数
- **悬浮窗设计**：无边框可拖动窗口，支持置顶、边缘拖拽调整大小
- **OCR 文字识别**：基于 Tesseract 引擎，支持中英文
- **翻译历史**：本地保存最近 100 条记录，支持搜索和复制
- **翻译缓存**：7 天有效期，避免重复请求
- **翻译评分**：智能评估译文质量，从长度比、多样性、格式、平滑度等多维度打分
- **多语言界面**：支持中文/英文切换
- **深色模式**：参考 Windows 11 Mica 设计语言
- **全局快捷键**：Ctrl+Shift+T 快速呼出悬浮窗
- **语音朗读**：Windows SAPI 朗读原文/译文
- **翻译通知**：悬浮窗隐藏时自动弹出 Toast 气泡显示译文
- **首次运行引导**：新用户首次启动自动进入配置向导

## 技术栈

- C# / .NET 8 WPF + HandyControl + CommunityToolkit.Mvvm
- MVVM 架构 + 依赖注入 (Microsoft.Extensions.DependencyInjection)
- 翻译引擎：百度翻译 API / Google Translate / Bing Translate / DeepL API
- AI 翻译：OpenAI 兼容 API / Anthropic Messages API / Ollama
- OCR：Tesseract 5.2.0
- 代码质量：SonarCloud 静态分析
- CI/CD：GitHub Actions 自动构建和扫描

## 运行方式

```powershell
# 构建
dotnet build src/TranslateTool.csproj --configuration Release

# 运行
dotnet run --project src/TranslateTool.csproj

# 运行测试
dotnet test src/Tests/TranslateTool.Tests.csproj
```

## 项目结构

```
src/
├── Models/          — 数据模型（AppSettings, LlmProvider, TranslationResult 等）
├── Services/        — 翻译引擎、OCR、缓存、评分等服务
├── ViewModels/      — MVVM ViewModel 层（CommunityToolkit.Mvvm）
├── Views/           — WPF 窗口界面（悬浮窗、设置、首次引导等）
├── Themes/          — 浅色/深色主题
├── Converters/      — 值转换器
├── Localization/    — 中英文多语言资源
├── Utils/           — 工具类（NativeMethods, UserDataPaths 等）
└── Tests/           — xUnit 单元测试
```

## 翻译引擎配置

| 引擎 | 申请地址 | 免费额度 |
|------|----------|----------|
| 百度翻译 | https://fanyi-api.baidu.com/product/11 | 200 万字符/月 |
| DeepL | https://www.deepl.com/pro-api | 50 万字符/月 |
| AI 翻译 | 支持 Ollama / OpenAI / OpenRouter 等 | 本地免费 / 按 API 计费 |

### AI 翻译供应商

| 供应商 | API 格式 | 默认地址 | 说明 |
|--------|----------|----------|------|
| Ollama（默认） | OpenAI Compatible | `http://localhost:11434/v1` | 本地运行，推荐 Gemma 4 E4B |
| OpenAI | OpenAI Compatible | `https://api.openai.com/v1` | GPT-4o / GPT-4o-mini |
| OpenRouter | OpenAI Compatible | `https://openrouter.ai/api/v1` | 多模型聚合 |
| DeepSeek | OpenAI Compatible | `https://api.deepseek.com/v1` | DeepSeek-V3 |
| SiliconFlow | OpenAI Compatible | `https://api.siliconflow.cn/v1` | 国产模型聚合 |
| Anthropic | Anthropic | `https://api.anthropic.com/v1` | Claude 系列 |

## 打包发布

### Inno Setup 安装包

```powershell
# 需要先安装 Inno Setup，然后运行：
iscc installer.iss
```

### Chocolatey

```powershell
cd packaging/chocolatey
choco pack
choco push TranslateTool.<version>.nupkg
```

### 代码签名

```powershell
.\scripts\sign.ps1 -FilePath "src\bin\Release\net8.0-windows\TranslateTool.exe" `
                    -CertificatePath "C:\cert\TranslateTool.pfx" `
                    -CertificatePassword (Read-Host "PFX Password" -AsSecureString)
```

## CI/CD

- **GitHub Actions** 自动构建和 SonarCloud 代码扫描
- 推送到 `main` 分支自动触发
- Quality Gate 不通过时 PR 检查会失败

## License

MIT
