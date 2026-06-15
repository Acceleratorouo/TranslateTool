# 下载 Tesseract 训练数据脚本
# 运行: .\download-tessdata.ps1

$tessdataDir = Join-Path $PSScriptRoot "tessdata"

if (!(Test-Path $tessdataDir)) {
    New-Item -ItemType Directory -Force -Path $tessdataDir | Out-Null
}

Write-Host "正在下载 Tesseract 训练数据..." -ForegroundColor Cyan
Write-Host "目标目录: $tessdataDir" -ForegroundColor Gray

# 下载简体中文训练数据
$chiUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/chi_sim.traineddata"
$chiFile = Join-Path $tessdataDir "chi_sim.traineddata"

if (!(Test-Path $chiFile)) {
    Write-Host "下载简体中文语言包 (chi_sim.traineddata)..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $chiUrl -OutFile $chiFile -UseBasicParsing
        Write-Host "✓ 中文语言包下载完成" -ForegroundColor Green
    } catch {
        Write-Host "✗ 下载失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "请手动下载: $chiUrl" -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ 中文语言包已存在" -ForegroundColor Green
}

# 下载英文训练数据
$engUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
$engFile = Join-Path $tessdataDir "eng.traineddata"

if (!(Test-Path $engFile)) {
    Write-Host "下载英文语言包 (eng.traineddata)..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $engUrl -OutFile $engFile -UseBasicParsing
        Write-Host "✓ 英文语言包下载完成" -ForegroundColor Green
    } catch {
        Write-Host "✗ 下载失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "请手动下载: $engUrl" -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ 英文语言包已存在" -ForegroundColor Green
}

Write-Host "`n下载完成！" -ForegroundColor Cyan
Write-Host "现在可以运行翻译工具使用 OCR 功能。" -ForegroundColor Gray
