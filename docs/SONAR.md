# SonarQube / SonarCloud 集成（SONAR）

> 适用版本：v1.x · 最后更新：2026-06-17

本文档说明如何将 TranslateTool 接入 SonarQube / SonarCloud 进行静态代码分析。

---

## 1. SonarQube vs SonarCloud

| 平台 | 适用 | 价格 |
|------|------|------|
| **SonarCloud** | 开源项目（推荐） | 公共仓库免费 |
| **SonarQube Server** | 自建、企业内部 | 社区版免费 |

TranslateTool 推荐使用 **SonarCloud**（项目本身就是开源友好的）。

---

## 2. 一次性配置

### 2.1 在 SonarCloud 上创建项目

1. 登录 <https://sonarcloud.io>（用 GitHub 账号）
2. 点击 **+** → **Analyze new project** → 选择 `TranslateTool/TranslateTool`
3. 生成 **Organization Key** 和 **Project Key**，更新到 `sonar-project.properties`
4. 在 **My Account → Security** 生成 token（保存到 GitHub Secrets）

### 2.2 在 GitHub 仓库配置 Secrets

| Secret 名称 | 内容 |
|------------|------|
| `SONAR_TOKEN` | SonarCloud 生成的 token |

### 2.3 启用 `dotnet-sonarscanner`

`dotnet-sonarscanner` 是 Sonar 官方提供的 .NET 工具：

```powershell
dotnet tool install --global dotnet-sonarscanner
```

无需在仓库内提交 binary，CI 流程会临时安装。

---

## 3. 本地运行（开发者手动）

```powershell
# 1. 开始扫描（从 clean 开始）
dotnet sonarscanner begin `
  /k:"TranslateTool_TranslateTool" `
  /d:sonar.login="$env:SONAR_TOKEN" `
  /d:sonar.host.url="https://sonarcloud.io"

# 2. 还原 + 构建
dotnet build "src/TranslateTool.csproj" --configuration Release

# 3. 跑测试并收集覆盖率
dotnet test "src/Tests/TranslateTool.Tests.csproj" `
  --collect:"XPlat Code Coverage" `
  --results-directory:TestResults `
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# 4. 结束扫描
dotnet sonarscanner end /d:sonar.login="$env:SONAR_TOKEN"
```

完成后访问 <https://sonarcloud.io/dashboard?id=TranslateTool_TranslateTool> 查看报告。

---

## 4. CI 集成（GitHub Actions）

[.github/workflows/sonar.yml](packaging/github-actions/../.github/workflows/sonar.yml) 提供了完整 workflow。

触发条件：
- push 到 `main` 分支
- Pull Request
- 每日定时扫描（cron）

```yaml
# 简版
- uses: sonarsource/sonarcloud-github-action@v2
  env:
    SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
  with:
    args: >
      -Dsonar.cs.opencover.reportPaths=src/Tests/coverage.opencover.xml
```

---

## 5. 质量门禁（Quality Gate）

### 5.1 默认规则

- 新代码覆盖率 ≥ 80%
- 新代码零 Bug
- 新代码零漏洞
- 重复率 ≤ 3%
- 代码异味密度 < 0.5/千行

### 5.2 严重等级处理策略

| 等级 | 处理 |
|------|------|
| Blocker | 立即修复，PR 必拒 |
| Critical | 24 小时内修复 |
| Major | 本周内修复 |
| Minor | 顺手修复 |

---

## 6. 与 .NET 内置分析的协同

SonarQube 与 `dotnet format` / Roslyn 分析器可互补，建议同时启用：

- Roslyn 分析器（IDE 即时反馈）
- `dotnet format` (CI 风格检查)
- SonarQube (深度 + 历史趋势 + 覆盖率)

详见 [CODE_REVIEW_CHECKLIST.md](./CODE_REVIEW_CHECKLIST.md) 第 9 节"测试"。

---

## 7. 排除规则说明

`sonar-project.properties` 中排除的目录：

| 排除 | 原因 |
|------|------|
| `src/bin/`, `src/obj/` | 构建产物 |
| `**/*.designer.cs`, `**/*.g.cs` | 自动生成代码 |
| `src/Resources/**` | 静态资源 |
| `src/Themes/**` | XAML 主题，非 C# 逻辑 |

如需分析 XAML 的 C# code-behind 中的逻辑，已被默认包含。

---

## 8. 覆盖率门槛建议

- 总体覆盖率 ≥ 70%（P0-P2 已完成项目基线）
- 核心服务（`Services/`、`Models/`）覆盖率 ≥ 85%
- ViewModel 覆盖率 ≥ 60%
- View（XAML）覆盖率不强制

---

## 9. 关联文档

- [CODE_REVIEW_CHECKLIST.md](./CODE_REVIEW_CHECKLIST.md) — 代码审查清单
- [PACKAGING.md](./PACKAGING.md) — 打包与分发
- [SECURITY.md](./SECURITY.md) — 安全策略
