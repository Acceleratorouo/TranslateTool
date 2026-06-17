<#
.SYNOPSIS
    使用 Authenticode 证书对 Windows 可执行文件签名。

.DESCRIPTION
    支持单个文件、整个目录或 glob 模式。自动调用时间戳服务。

.PARAMETER FilePath
    要签名的文件或目录路径。

.PARAMETER CertificatePath
    证书 .pfx 文件路径。

.PARAMETER CertificatePassword
    证书密码（SecureString）。

.PARAMETER TimestampUrl
    RFC 3161 时间戳服务 URL。

.PARAMETER SignToolPath
    signtool.exe 完整路径。默认自动从 Windows SDK 搜索。

.PARAMETER Description
    签名描述（嵌入到签名中）。

.EXAMPLE
    .\sign.ps1 -FilePath "src\bin\Release\net8.0-windows\TranslateTool.exe" `
               -CertificatePath "C:\cert\TranslateTool.pfx" `
               -CertificatePassword (Read-Host "PFX Password" -AsSecureString) `
               -TimestampUrl "http://timestamp.digicert.com"

.EXAMPLE
    .\sign.ps1 -FilePath "src\bin\Release\net8.0-windows" `
               -CertificatePath "C:\cert\TranslateTool.pfx" `
               -CertificatePassword $securePwd `
               -Recursive

.NOTES
    Author:  TranslateTool
    Version: 1.0.0
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$FilePath,

    [Parameter(Mandatory)]
    [string]$CertificatePath,

    [Parameter(Mandatory)]
    [SecureString]$CertificatePassword,

    [Parameter(Mandatory = $false)]
    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [Parameter(Mandatory = $false)]
    [string]$SignToolPath,

    [Parameter(Mandatory = $false)]
    [string]$Description = "TranslateTool",

    [Parameter(Mandatory = $false)]
    [switch]$Recursive
)

$ErrorActionPreference = "Stop"

# 1. 定位 signtool.exe
if (-not $SignToolPath) {
    $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (-not (Test-Path $kitsRoot)) {
        throw "未找到 Windows SDK（$kitsRoot）。请先安装 https://developer.microsoft.com/windows/downloads/windows-sdk/"
    }
    $candidates = Get-ChildItem -Path $kitsRoot -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
                  Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } |
                  Sort-Object FullName -Descending
    if (-not $candidates) {
        throw "未在 $kitsRoot 下找到 signtool.exe"
    }
    $SignToolPath = $candidates[0].FullName
    Write-Host "使用 signtool: $SignToolPath"
}

# 2. 校验证书存在
if (-not (Test-Path $CertificatePath)) {
    throw "证书文件不存在: $CertificatePath"
}

# 3. 转换 SecureString 密码为明文（signtool 不支持 SecureString）
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($CertificatePassword)
$plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)

# 4. 收集待签文件
$targets = @()
if ((Get-Item $FilePath).PSIsContainer) {
    $files = Get-ChildItem -Path $FilePath -Filter "*.exe" -Recurse:$Recursive
    $dlls  = Get-ChildItem -Path $FilePath -Filter "*.dll" -Recurse:$Recursive
    $targets = $files + $dlls
} else {
    $targets = @(Get-Item $FilePath)
}

if ($targets.Count -eq 0) {
    Write-Warning "未找到待签名文件"
    exit 0
}

Write-Host "准备签名 $($targets.Count) 个文件..."

# 5. 执行签名
$failed = @()
foreach ($file in $targets) {
    Write-Host "  → $($file.FullName)" -NoNewline
    $args = @(
        "sign"
        "/fd", "SHA256"           # 签名算法
        "/a"                      # 自动选证书
        "/f", "`"$CertificatePath`""   # 证书路径
        "/p", "`"$plainPassword`""     # 密码
        "/t", "`"$TimestampUrl`""      # 时间戳
        "/d", "`"$Description`""       # 描述
        "`"$($file.FullName)`""
    )
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $SignToolPath
    $psi.Arguments = ($args -join " ")
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.UseShellExecute        = $false
    $psi.CreateNoWindow         = $true

    $proc = [System.Diagnostics.Process]::Start($psi)
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()

    if ($proc.ExitCode -eq 0) {
        Write-Host "  ✓" -ForegroundColor Green
    } else {
        Write-Host "  ✗ (ExitCode=$($proc.ExitCode))" -ForegroundColor Red
        if ($stderr) { Write-Host "    错误: $stderr" -ForegroundColor Red }
        $failed += $file.FullName
    }
}

# 6. 清理敏感内存
$plainPassword = $null
[System.GC]::Collect()

# 7. 总结
if ($failed.Count -gt 0) {
    Write-Host "`n失败的签名 ($($failed.Count)):" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "`n✓ 全部 $($targets.Count) 个文件签名完成" -ForegroundColor Green
