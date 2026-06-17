# 代码签名与证书（SIGNING）

> 适用版本：v1.x · 最后更新：2026-06-17

本文档说明 TranslateTool 的代码签名策略、证书申请流程、签名脚本与 CI 集成。

---

## 1. 为什么需要代码签名

- **Windows SmartScreen**：未签名的程序在首次运行时弹出蓝色警告，严重影响用户首次体验
- **winget / Chocolatey**：两者都**强制要求**安装包必须带有效的 Authenticode 签名
- **企业部署**：IT 部门通常禁止执行未签名的可执行文件
- **品牌信任**：签名信息（公司名 + 时间戳）能阻止攻击者将恶意软件伪装成我们的应用

---

## 2. 证书选型

### 2.1 三种选择

| 类型 | 信任级别 | 价格 | 适用 |
|------|---------|------|------|
| **EV（扩展验证）** 代码签名 | 立即获得 SmartScreen 信任 | 约 300-500 USD/年 | 商业产品（推荐） |
| **OV（普通验证）** 代码签名 | 需积累下载量才能消除 SmartScreen | 约 70-200 USD/年 | 开源项目、起步阶段 |
| **自签证书** | 仅内网信任 | 0 | 仅内部分发（**不推荐**外部分发） |

### 2.2 推荐供应商

- **Certum**（波兰，提供免费 1 年代码签名证书，需 Open Source 验证）：<https://shop.certum.eu/open-source-code-signing-certificate.html>
- **SSL.com**（按月/年订阅）：<https://www.ssl.com/certificates/code-signing/>
- **DigiCert**（传统大厂）：<https://www.digicert.com/signing/code-signing-certificates.htm>
- **SignPath.io**（开源免费，签 CI 产物）：<https://signpath.io>（推荐用于 CI）

---

## 3. 申请流程（以 Certum 为例）

1. 注册账号并选择 "Open Source Code Signing"
2. 提交项目 GitHub 仓库 URL
3. 验证仓库所有权（在 README 中添加校验 token）
4. 下载证书（`.pfx` 文件 + 密码）
5. **私钥必须安全存储**：
   - 推荐：Azure Key Vault / HashiCorp Vault
   - 可接受：加密 U 盘 + 密码保险箱
   - **绝不可**提交到 Git 仓库

---

## 4. 本地签名（开发者手动）

### 4.1 安装 Windows SDK

下载 [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)，安装时勾选 "Windows SDK Signing Tools"。

signtool 路径：`C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\signtool.exe`

### 4.2 签名脚本

[scripts/sign.ps1](../scripts/sign.ps1) 提供了 PowerShell 签名脚本：

```powershell
# 签名单个 exe
.\scripts\sign.ps1 `
  -FilePath "src\bin\Release\net8.0-windows\TranslateTool.exe" `
  -CertificatePath "$env:USERPROFILE\cert\TranslateTool.pfx" `
  -CertificatePassword (Read-Host "PFX 密码" -AsSecureString) `
  -TimestampUrl "http://timestamp.digicert.com"
```

### 4.3 在 Inno Setup 中集成

在 `installer.iss` 顶部加入：

```iss
#define SignTool "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
#define PFXPath "C:\secure\TranslateTool.pfx"
#define PFXPassword "..."
```

在 `[Files]` 段后加入：

```iss
[Signtool]
Sign: TranslateTool.exe; Source: "..."; DestDir: "{app}"
```

更推荐在 `Build` 阶段用 signtool 直接对编译产物签名，再交给 Inno Setup 打包。

---

## 5. CI 签名（GitHub Actions 推荐）

[packaging/github-actions/sign-and-release.yml](packaging/github-actions/sign-and-release.yml) 提供了完整 workflow：

1. tag 推送触发
2. 构建 self-contained 发布版
3. 从 GitHub Secrets 读取 `.pfx` base64 和密码
4. 使用 signtool 签名
5. 创建 GitHub Release 并上传产物

**GitHub Secrets 配置**（仓库 Settings → Secrets）：

| Secret 名称 | 内容 |
|------------|------|
| `CODE_SIGN_PFX_BASE64` | 证书 .pfx 文件的 base64 编码（`[Convert]::ToBase64String((Get-Content cert.pfx -Encoding Byte))`） |
| `CODE_SIGN_PFX_PASSWORD` | 证书密码 |

---

## 6. 验证签名

### 6.1 在 Windows 上查看

```powershell
# 查看签名信息
Get-AuthenticodeSignature "src\bin\Release\net8.0-windows\TranslateTool.exe" |
    Format-List *

# 验证签名有效
if ((Get-AuthenticodeSignature "TranslateTool.exe").Status -eq "Valid") {
    Write-Host "✓ 签名有效" -ForegroundColor Green
}
```

### 6.2 在 winget 提交前验证

```powershell
wingetcreate validate manifests/t/TranslateTool/TranslateTool/1.0.0/
# 验证 manifest 中的 SignatureSha256 与实际一致
```

---

## 7. 吊销与轮换

- **私钥泄露**：立即联系 CA 吊销证书，申请新证书
- **证书到期**：在到期前 30 天开始轮换流程
- **多证书并存**：winget 接受安装包附带多个签名（直到旧证书过期），可保证升级平滑

---

## 8. 时间戳服务

签名时**必须**使用时间戳 URL，否则证书过期后签名会失效：

| CA | 时间戳 URL |
|----|-----------|
| DigiCert | `http://timestamp.digicert.com` |
| Sectigo | `http://timestamp.sectigo.com` |
| Certum | `http://time.certum.pl` |
| GlobalSign | `http://timestamp.globalsign.com/tsa/r6advanced1` |

---

## 9. 与 winget / Chocolatey 的关系

- winget **强制** `SignatureSha256` 字段（见 [manifest](../packaging/winget/TranslateTool.installer.yaml)）
- Chocolatey 在审核时检查 Authenticode 签名，无签名会被拒

详见 [PACKAGING.md](./PACKAGING.md)。

---

## 10. 关联文档

- [PACKAGING.md](./PACKAGING.md) — 打包与分发
- [SECURITY.md](./SECURITY.md) — 安全策略
