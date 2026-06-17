$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

# 包元数据
$packageName    = 'TranslateTool'
$installerType  = 'exe'
$silentArgs     = '/SILENT /SP- /NORESTART /CLOSEAPPLICATIONS'
$url            = 'https://github.com/TranslateTool/TranslateTool/releases/download/v1.1.0/TranslateTool_Setup_1.1.0.exe'
$url64bit       = $url
$checksum       = '0000000000000000000000000000000000000000000000000000000000000000'
$checksumType   = 'sha256'
$checksum64     = $checksum
$checksumType64 = 'sha256'

# 安装前关闭运行中的实例（如有）
$proc = Get-Process -Name 'TranslateTool' -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "正在关闭运行中的 TranslateTool 进程..."
    $proc | Stop-Process -Force
    Start-Sleep -Seconds 1
}

Install-ChocolateyPackage `
    -PackageName $packageName `
    -FileType $installerType `
    -SilentArgs $silentArgs `
    -Url $url `
    -Url64bit $url64bit `
    -Checksum $checksum `
    -ChecksumType $checksumType `
    -Checksum64 $checksum64 `
    -ChecksumType64 $checksumType64

# 安装后可选：创建桌面快捷方式（如果用户偏好）
# 已由 Inno Setup 的 [Icons] 段处理

Write-Host "TranslateTool 安装完成！"
