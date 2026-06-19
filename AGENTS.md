# TranslateTool Agent Instructions

本文件给 Codex、Claude 和其他自动化代理使用。开始任何修改前先读 `README.md` 与 `MEMORY.md`；涉及发布、安全、隐私或打包时，再读 `docs/` 下对应文档。

## 项目事实

- 这是 .NET 8 WPF 桌面应用，主工程在 `src/TranslateTool.csproj`，测试工程在 `src/Tests/TranslateTool.Tests.csproj`。
- 架构以 MVVM 为主：`Views` 只处理 WPF 交互，业务逻辑放在 `ViewModels` / `Services`，数据结构放在 `Models`。
- 用户设置位于 `%APPDATA%\TranslateTool\settings.json`；缓存、历史、OCR 数据和日志位于 `%LOCALAPPDATA%\TranslateTool\...`，不要写入安装目录。
- 当前翻译引擎标识为 `baidu`、`google`、`microsoft`、`deepl`、`ai`。AI 翻译不参与多引擎对比，除非用户明确要求改变产品行为。
- 敏感设置通过 `SensitiveSettingsProtector` 使用 Windows DPAPI 保护，旧明文配置会在下次保存时迁移。

## 开发规则

- 修改前运行 `git status --short`，不要覆盖用户已有未提交改动。
- 文档和代码必须一致；实现未完成时，不要在安全、隐私或发布文档中写成已完成。
- 新增用户可见字符串必须同步 `src/Localization/Strings.zh-CN.resx` 与 `src/Localization/Strings.en-US.resx`，代码中优先通过 `LocalizationManager` 读取。
- API Key、Token、Secret 不得写入日志、测试快照、示例配置或提交记录。
- 文件路径使用 `UserDataPaths`，不要在业务代码中散落 `%APPDATA%`、`%LOCALAPPDATA%` 或安装目录路径。
- 网络请求优先复用既有策略：共享请求走 `HttpShared.Client`；供应商级 LLM 客户端如需自建 `HttpClient`，必须设置超时、释放资源，并避免泄露请求/响应体中的敏感信息。
- 避免新增静默 `catch { }`。确实可忽略的异常要说明原因；用户数据保存、网络、OCR、翻译失败应返回可诊断信息。
- UI 线程不能被 `.Result`、`.Wait()`、`Thread.Sleep()` 或同步文件 I/O 阻塞。已有问题可逐步修复，但新增代码不要扩大。

## 架构约束

- `App.xaml.cs` 仍承担启动、托盘、热键、单实例、设置窗口等职责；新增启动逻辑前优先考虑拆到深模块。
- 热键字符串解析已迁到 `HotkeyParser`，不要再把新解析规则写回 `App.xaml.cs`。
- `FloatingWindowViewModel` 已有可注入 seam：`IClipboardService`、`IFilePickerService`、`IRegionSelectionService`。涉及剪贴板、文件选择、区域选择时优先使用这些接口并补测试。
- Toast/通知、计时器、SAPI 朗读仍是后续 seam 候选；改动时避免无测试的大重排。

## 测试与验证

- 常规验证：`dotnet test src/Tests/TranslateTool.Tests.csproj`。
- 发布前验证：`dotnet build src/TranslateTool.csproj --configuration Release`，再运行完整测试。
- 涉及 AI 翻译时至少运行：`dotnet test src/Tests/TranslateTool.Tests.csproj --filter "FullyQualifiedName~Llm|FullyQualifiedName~AiTranslator|FullyQualifiedName~EngineStatus"`。
- 涉及用户数据路径、设置保存、导入导出时运行 `UserDataPathsTests`、`UserDataServiceTests`、`AppSettingsTests`。

## 优先改进建议

1. 继续拆分 `App.xaml.cs` 的单实例、托盘菜单和启动编排职责。
2. 为 Toast/通知、计时器、SAPI 朗读建立可测试 seam。
3. 逐步消除同步 I/O、静默异常和 CA analyzer warning；当前 warning-free 依赖基线抑制，不代表源代码问题已全部修复。
4. 继续把用户可见硬编码文案迁移到 `.resx`，保持中英文同步。
