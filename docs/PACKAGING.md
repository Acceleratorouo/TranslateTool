# Winget / Chocolatey / 通用打包说明

本文档说明 TranslateTool 在多个分发渠道的打包流程，以及如何发布到 `winget-pkgs` 和 `chocolatey` 官方仓库。

---

## 1. 当前构建产物

应用是 .NET 8 WPF 程序，发布方式有三种：

| 渠道 | 产物 | 适用场景 |
|------|------|---------|
| Inno Setup | `installer/TranslateTool_Setup_x.y.z.exe` | 直接分发、企业内网 |
| Portable ZIP | `publish/TranslateTool-x.y.z-win-x64.zip` | U 盘、便携使用 |
| winget / chocolatey | 上述两种任一 | 包管理器 |

### 1.1 生成 portable zip

```powershell
& "C:\Users\86186\.dotnet\dotnet.exe" publish "src\TranslateTool.csproj" `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=false `
  -p:Version=1.0.0 `
  -o "publish/win-x64"
```

### 1.2 生成 Inlno Setup 安装包

需要本地安装 [Inno Setup 6](https://jrsoftware.org/isinfo.php)。运行：

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

输出：`installer/TranslateTool_Setup_1.0.0.exe`

---

## 2. winget 包

### 2.1 目录结构

winget 仓库 `winget-pkgs` 中的目录约定如下（PR 提交时需创建）：

```
manifests/t/TranslateTool/TranslateTool/1.0.0/
├── TranslateTool TranslateTool.installer.yaml
├── TranslateTool.locale.en-US.yaml
└── TranslateTool.locale.zh-CN.yaml
```

### 2.2 模板

- [packaging/winget/TranslateTool.installer.yaml](packaging/winget/TranslateTool.installer.yaml) — 安装器清单
- [packaging/winget/TranslateTool.locale.en-US.yaml](packaging/winget/TranslateTool.locale.en-US.yaml) — 英文元数据
- [packaging/winget/TranslateTool.locale.zh-CN.yaml](packaging/winget/TranslateTool.locale.zh-CN.yaml) — 中文元数据

### 2.3 验证与提交

```powershell
# 安装 wingetcreate（一次性）
winget install JanDeDobbeleer.wingetcreate

# 在仓库根目录运行：
wingetcreate validate manifests/t/TranslateTool/TranslateTool/1.0.0/
```

提交到 `microsoft/winget-pkgs` 的 PR：

1. Fork `https://github.com/microsoft/winget-pkgs`
2. 在 fork 中放入清单文件
3. PR 标题：`New package: TranslateTool version 1.0.0`
4. 等候 CI 通过 + 维护者审核

---

## 3. Chocolatey 包

### 3.1 目录结构

```
packaging/chocolatey/TranslateTool/
├── TranslateTool.nuspec
├── tools/
│   ├── chocolateyinstall.ps1
│   ├── chocolateyuninstall.ps1
│   └── LICENSE.txt
└── README.md
```

### 3.2 打包与发布

```powershell
# 安装 chocolatey（如未安装）
# 参见 https://chocolatey.org/install

# 打包
choco pack packaging/chocolatey/TranslateTool/TranslateTool.nuspec `
  --version=1.0.0 `
  --outputdirectory=build/

# 推送到社区仓库（需先注册账号并获取 API key）
choco push build/TranslateTool.1.0.0.nupkg `
  --source=https://push.chocolatey.org/ `
  --api-key=<YOUR_API_KEY>
```

### 3.3 用户侧安装命令

```powershell
# 官方源（审核通过后）
choco install translatetool

# 测试源（审核中）
choco install translatetool --source=https://chocolatey.org/api/v2/
```

---

## 4. CI 自动发布（推荐）

可使用 GitHub Actions 在 tag 推送时自动构建并发布到 winget/Chocolatey/Installer。参考：

- [packaging/github-actions/release.yml](packaging/github-actions/release.yml)

---

## 5. 包签名（与下一节"代码签名"配套）

winget 和 chocolatey 都要求安装包必须有有效的 Authenticode 签名。详见 [docs/SIGNING.md](./SIGNING.md)。
