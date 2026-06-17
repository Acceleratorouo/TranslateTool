#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

# Gemma 4 E4B 模型标签与 Ollama OpenAI 兼容端点
$ModelTag = 'gemma4:e4b'
$OllamaBaseUrl = 'http://localhost:11434/v1'

function Test-OllamaInstalled {
    try {
        $null = Get-Command ollama -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Install-Ollama {
    Write-Host '未检测到 Ollama。' -ForegroundColor Yellow
    Write-Host '请前往 https://ollama.com 下载并安装 Ollama，然后重新运行本脚本。' -ForegroundColor Cyan
    Start-Process 'https://ollama.com'
    return $false
}

function Deploy-Gemma4E4B {
    Write-Host "正在拉取 $ModelTag ..." -ForegroundColor Cyan
    ollama pull $ModelTag
    if ($LASTEXITCODE -ne 0) {
        throw "拉取模型 $ModelTag 失败"
    }

    Write-Host '验证模型...' -ForegroundColor Cyan
    $models = ollama list
    if ($models -notmatch $ModelTag) {
        throw "模型 $ModelTag 未在本地列表中找到"
    }

    Write-Host "✅ Gemma 4 E4B 部署完成" -ForegroundColor Green
    Write-Host "Base URL: $OllamaBaseUrl" -ForegroundColor Green
    Write-Host "Model:    $ModelTag" -ForegroundColor Green
    Write-Host ""
    Write-Host '请在 TranslateTool 设置 → AI 翻译 中确认 Ollama 供应商的 BaseUrl 为:' -ForegroundColor Cyan
    Write-Host "  $OllamaBaseUrl" -ForegroundColor White
    Write-Host "并在模型列表中填入: $ModelTag" -ForegroundColor Cyan
}

Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  TranslateTool - Gemma 4 E4B 本地部署' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ""

if (-not (Test-OllamaInstalled)) {
    if (-not (Install-Ollama)) {
        Write-Host '请安装 Ollama 后重新运行本脚本。' -ForegroundColor Yellow
        exit 1
    }
}

try {
    Deploy-Gemma4E4B
    Write-Host ""
    Write-Host '按任意键退出...' -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit 0
}
catch {
    Write-Host "❌ 部署失败: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host '按任意键退出...' -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    exit 1
}
