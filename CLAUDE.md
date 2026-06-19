# Claude Instructions for TranslateTool

遵循 `AGENTS.md` 中的项目规则；本文件只补充 Claude 类代理的工作方式约束。

## 工作方式

- 修改前先读 `MEMORY.md`，并用代码验证其中的关键事实。发现记忆与代码不一致时，优先在本次修改中修正记忆。
- 保持改动小而可验证。不要把功能实现、重构、文档清理、格式化混在一个提交范围内。
- 对 WPF/MVVM 改动，先确认数据绑定、命令、属性通知、线程切换和本地化资源，再写代码。
- 对架构建议使用一致术语：Module、Interface、Implementation、Depth、Seam、Adapter、Leverage、Locality。不要为了抽象而新增没有测试收益的浅模块。
- 对安全相关请求，不要只相信文档声明，必须检查实现。当前 API Key 加密由 `SensitiveSettingsProtector` 通过 Windows DPAPI 实现。

## 项目特定注意事项

- `AppSettings.Current` 是全局单例，测试和运行时状态容易互相影响。改相关代码时优先补隔离测试。
- `LlmProviderService` 当前是静态状态入口，默认供应商和启用状态都来自 `AppSettings.Current`。
- `Gemini` 原生 API 格式当前不支持；推荐用户使用 OpenAI 兼容端点，除非本次任务就是实现 Gemini 原生协议。
- `FloatingWindowViewModel` 是高风险文件，承担剪贴板监听、翻译、历史、Toast、多引擎对比、朗读等职责。已有 `IClipboardService`、`IFilePickerService`、`IRegionSelectionService` seam；新增改动优先复用并加回归测试。
- `App.xaml.cs` 已经承载启动、托盘、热键、单实例、设置窗口等职责。热键解析已迁到 `HotkeyParser`；新增启动逻辑前先考虑是否应拆出 Module。

## 输出要求

- 说明改了哪些文件、为什么改、如何验证。
- 如果没有运行测试，明确说明原因。
- 如果发现用户已有未提交改动，不要覆盖；最终回复中点明哪些文件原本已有改动。
