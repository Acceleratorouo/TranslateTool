$ErrorActionPreference = 'Stop'
$packageName = 'TranslateTool'

# 卸载前关闭运行中的实例
$proc = Get-Process -Name 'TranslateTool' -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "正在关闭运行中的 TranslateTool 进程..."
    $proc | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# 通过卸载注册表项定位卸载字符串
$uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{E8A3B5C7-D4F6-4A9B-8C2D-1E3F5A7B9C0D}_is1'
$uninstallString = (Get-ItemProperty -Path $uninstallKey -ErrorAction SilentlyContinue).UninstallString

if ($uninstallString) {
    Write-Host "使用卸载程序: $uninstallString"
    $uninstallArgs = '/SILENT /NORESTART /SUPPRESSMSGBOXES'
    Start-ChocolateyProcessAsAdmin -Statements "/C `"$uninstallString`" $uninstallArgs" -ExeToRun 'cmd.exe'
} else {
    Write-Warning "未找到卸载注册表项，可能未通过本包安装。"
}

# 清理用户数据（仅在用户明确要求时执行 — 通过 --params='--purge'）
$pp = Get-PackageParameters
if ($pp['purge']) {
    Write-Host "清理用户数据..."
    $appData = Join-Path $env:APPDATA 'TranslateTool'
    $localAppData = Join-Path $env:LOCALAPPDATA 'TranslateTool'
    foreach ($dir in @($appData, $localAppData)) {
        if (Test-Path $dir) {
            Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "已删除: $dir"
        }
    }
}

Write-Host "TranslateTool 卸载完成！"
