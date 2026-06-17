# 安全性策略（SECURITY）

本文档说明 TranslateTool 在用户数据隔离、敏感数据保护、输入校验等方面的策略。

## 1. 用户数据隔离

TranslateTool **绝不将任何用户数据写入应用安装目录**。所有用户级数据均放在标准的 Windows 用户目录下，实现多用户隔离、可备份、可迁移。

### 1.1 目录布局

| 数据类型 | 路径 | 漫游？ |
|---------|------|--------|
| 应用设置 | `%APPDATA%\TranslateTool\settings.json` | ✅ 漫游（可经 OneDrive 跨设备） |
| 翻译缓存 | `%LOCALAPPDATA%\TranslateTool\Cache\translation_cache.json` | ❌ 本地 |
| 翻译历史 | `%LOCALAPPDATA%\TranslateTool\History\translation_history.json` | ❌ 本地 |
| OCR 语言包 | `%LOCALAPPDATA%\TranslateTool\tessdata\` | ❌ 本地 |
| 日志 | `%LOCALAPPDATA%\TranslateTool\Logs\` | ❌ 本地 |

### 1.2 实现细节

所有路径集中在 [`src/Utils/UserDataPaths.cs`](../src/Utils/UserDataPaths.cs) 工具类中管理。应用启动时调用 `UserDataPaths.Initialize()` 自动创建缺失的目录。

历史/缓存/设置在写入前都会调用 `UserDataPaths.EnsureDirectoryExists(...)`，避免在首次运行或目录被外部清理后崩溃。

### 1.3 为何不放在安装目录

- **违反 Windows 最佳实践**：安装目录（`Program Files`）默认无写权限，且每用户共用。
- **多用户冲突**：同一台机器多用户共享时，安装目录的数据会被覆盖。
- **卸载残留**：包管理器升级或卸载时，存放在安装目录的数据可能丢失。
- **权限提升风险**：在受保护目录写文件需要管理员权限，可能触发 UAC 弹窗。

## 2. 敏感数据保护

### 2.1 API Key 加密

百度 AppID/SecretKey、DeepL API Key 等敏感字段使用 **Windows DPAPI** (`ProtectedData.Protect`) 加密后存入 `settings.json`。详见 `AppSettings.EncryptSensitiveFields()`。

解密使用 `CurrentUser` 作用域，仅当前 Windows 用户可解密，跨用户无法读取。

### 2.2 不在日志中泄露

- 日志中**禁止**打印 API Key、Token 等敏感字段。
- 网络异常时只记录 URL 和状态码，不打印请求/响应体。

## 3. 网络安全

### 3.1 HTTPS 强制

所有翻译 API 调用均使用 HTTPS。代码中不存在 `http://` 形式的硬编码 URL。

### 3.2 TLS 最低版本

默认使用 .NET 运行时配置（TLS 1.2 及以上）。不绕过证书验证（`ServerCertificateCustomValidationCallback` 不返回 `true`）。

## 4. 输入校验

- 剪贴板文本长度限制：默认 10,000 字符（防止恶意大文本耗尽内存/触发后端限流）。
- 文件翻译：限制单文件 ≤ 50 MB，超出弹窗提示。
- API Key 格式：保存前正则校验（如 DeepL Key 必须以 `uxxx-` 或相似格式开头）。
- 文件名：使用 `Path.GetFileName` 防止路径穿越。

## 5. 依赖与构建

- 所有 NuGet 依赖固定最低版本，定期运行 `dotnet list package --vulnerable --include-transitive`。
- 启用 NuGetAudit：`NuGetAuditMode=direct`。
- 关键依赖锁定在 `Directory.Packages.props`（central package management）。

## 6. 报告漏洞

如发现安全问题，请通过 GitHub Issues 或邮件联系维护者，**不要**在公开 Issue 中贴 PoC。

## 7. 关联文档

- [`PRIVACY.md`](./PRIVACY.md) — 隐私政策（GDPR 合规）
- [`CODE_REVIEW_CHECKLIST.md`](./CODE_REVIEW_CHECKLIST.md) — 安全审查清单
