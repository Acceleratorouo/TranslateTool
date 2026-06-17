# TranslateTool — 包管理器分发

本目录包含 TranslateTool 在 Windows 包管理器的清单文件。

## winget

提交到 `microsoft/winget-pkgs` 仓库：
https://github.com/microsoft/winget-pkgs/tree/master/manifests/t/TranslateTool/TranslateTool

详见 [../../docs/PACKAGING.md](../../docs/PACKAGING.md)。

## Chocolatey

本地打包命令：

```powershell
choco pack packaging/chocolatey/TranslateTool/TranslateTool.nuspec `
  --version=1.0.0 `
  --outputdirectory=build/
```

详见 [../../docs/PACKAGING.md](../../docs/PACKAGING.md)。
