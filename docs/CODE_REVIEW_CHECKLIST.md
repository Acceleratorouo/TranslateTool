# Code Review Checklist — TranslateTool

> 本清单用于 PR 合并前的代码审查。所有 PR 必须逐项过一遍，重要项必须勾选或说明不适用。

---

## 1. 功能性（必做）

- [ ] PR 描述中清晰说明了修改动机和解决的具体问题
- [ ] 行为与需求/issue/PRD 一致，无范围外的功能混入
- [ ] 已覆盖正常路径和异常路径（含空值、null、超长字符串、并发场景）
- [ ] 涉及 UI 改动的附了截图或录屏
- [ ] 涉及翻译引擎/API 改动的附了真实请求样例（请求/响应）

## 2. 架构与设计

- [ ] 改动遵循 MVVM：View 不直接调用 Service，全部走 ViewModel
- [ ] 新 Service 通过 `IServiceCollection` 注册到 DI 容器（[App.xaml.cs](file:///e:/vibe%20coding/3/src/App.xaml.cs)）
- [ ] 跨模块依赖面向接口（`ITranslator`、`IOcrService`、`ITranslationCache`），不直接 new 具体类
- [ ] 单例/Scoped/Transient 生命周期选择正确
- [ ] 状态变化通过 `INotifyPropertyChanged` / `[ObservableProperty]` 通知，避免轮询
- [ ] 没有把"全局"状态硬编码到 View 上

## 3. 异步与线程

- [ ] 所有 `I/O` 和 `await` 调用都使用 `async/await`，没有 `.Result` / `.Wait()` 阻塞 UI 线程
- [ ] `CancellationToken` 沿调用链传透，组件销毁时取消未完成任务
- [ ] 多线程访问的可变状态用 `lock` / `ConcurrentXxx` 保护
- [ ] `ConfigureAwait(false)` 用于类库代码，UI 代码用默认
- [ ] `Dispatcher` 调度：非 UI 线程更新绑定属性时已切回 UI 线程

## 4. 错误处理

- [ ] 异常被捕获并以用户可理解的形式呈现，不吞掉原始堆栈
- [ ] 日志记录关键错误（含异常类型、消息、上下文）
- [ ] 翻译引擎 / 网络 / OCR 失败的回退策略已实现并测试
- [ ] 验证输入（API Key 格式、文件大小、URL 合法性）
- [ ] 没有把异常信息直接显示给最终用户（含堆栈）

## 5. 资源与性能

- [ ] `HttpClient` 复用 `HttpShared` 单例，未新建 `new HttpClient()`
- [ ] 图片上传/截图前压缩，避免 OOM
- [ ] 大列表用虚拟化（`VirtualizingStackPanel`）或分页
- [ ] 缓存命中路径有日志或指标
- [ ] 没有 N+1 序列化/反序列化

## 6. 安全

- [ ] API Key 走 `EncryptionService` 加密落盘，明文不写日志/文件
- [ ] 所有外发请求使用 HTTPS（[AppSettings.cs](file:///e:/vibe%20coding/3/src/Models/AppSettings.cs) `RequireHttps = true`）
- [ ] 频率限制（`RateLimiter`）已应用到用户输入触发的 API
- [ ] 剪贴板读取做内容长度上限（防止异常卡死）
- [ ] 没有硬编码密钥、Token、个人信息
- [ ] 用户数据存储目录遵循 [用户数据隔离规范](file:///e:/vibe%20coding/3/docs/SECURITY.md)

## 7. UI / 无障碍

- [ ] 所有按钮/输入框有 `AutomationProperties.Name` 与 `HelpText`
- [ ] 关键动态文本设置 `AutomationProperties.LiveSetting="Polite"`
- [ ] Tab 顺序符合视觉顺序，可达性经过键盘测试
- [ ] 颜色对比度满足 WCAG AA（正文 ≥ 4.5:1，大字 ≥ 3:1）
- [ ] 错误提示以可读文本+图标呈现，不只靠颜色
- [ ] 焦点状态可见（已应用 `ClaudeFocusVisual`）

## 8. 国际化

- [ ] 用户可见字符串走 `.resx`，没有硬编码中英文字符串
- [ ] 数字、日期使用 `CultureInfo.InvariantCulture`（API 请求）或 `CurrentCulture`（UI 显示）
- [ ] 不假设文本方向，文案在 RTL 语言下也通顺
- [ ] 新增 UI 字符串同步到 `Strings.zh-CN.resx` 和 `Strings.en-US.resx`

## 9. 测试

- [ ] 新增 Service / ViewModel 含单元测试（`src/Tests/`）
- [ ] 关键路径有集成测试（端到端翻译流程）
- [ ] 修复 bug 时附了回归测试（先重现、再修复）
- [ ] 全部测试通过：`dotnet test`
- [ ] 没有跳过测试或注释掉断言
- [ ] 测试命名遵循 `Method_State_Expectation` 模式

## 10. 代码风格

- [ ] 通过 `dotnet format` 和 EditorConfig 规则
- [ ] 没有 IDE 警告（CA 规则启用）
- [ ] 公共 API 含 XML 文档注释（`<summary>`、`<param>`、`<returns>`）
- [ ] 无明显魔法数字，常量有名字和注释
- [ ] 单文件不超过 ~500 行（超长说明需要拆分）
- [ ] 没有 `TODO` / `FIXME` 留在合并后的代码里（必须转 issue）

## 11. 依赖与构建

- [ ] 新增 NuGet 包有理由说明，并使用稳定版本
- [ ] 没引入不兼容 License 的包
- [ ] `dotnet build -c Release` 通过，无 warning
- [ ] 跨平台路径/IO 用 `Path.Combine` / `File.*Async`
- [ ] 资源文件（图标、字典）走 `.csproj` 的 `Resource` 引用

## 12. 文档

- [ ] 用户可见的行为变更同步到 README / MEMORY.md
- [ ] 新增 Service / ViewModel 出现在 [CODE_WIKI.md](file:///e:/vibe%20coding/3/CODE_WIKI.md) 模块清单
- [ ] 架构决策有 ADR（`docs/adr/`）记录
- [ ] 破坏性变更在 PR 描述用 ⚠️ 标记

---

## 审查流程

1. **作者自查**：提交 PR 前按本清单 1~12 自查，逐项打勾或说明
2. **审查者**：至少 1 人 review，修改超过 300 行需要 2 人
3. **CI 自动化**：`dotnet test`、`dotnet format --verify-no-changes`、CodeQL 必须通过
4. **合并策略**：squash merge，主干必须始终可发布

## 不通过常见原因

- ❌ 缺少测试或测试只覆盖 happy path
- ❌ 新增 UI 文本未走资源文件
- ❌ 异常处理只 `catch (Exception) {}`
- ❌ 阻塞 UI 线程（`Task.Result`、`Thread.Sleep`）
- ❌ PR 描述为空或只贴 stack trace
- ❌ 把多个不相关修改塞进同一 PR

---

最后更新：2026-06-16
