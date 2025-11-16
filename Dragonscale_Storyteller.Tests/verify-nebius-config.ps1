# Nebius AI Configuration Verification Script
# This script verifies that Nebius AI is correctly configured

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Nebius AI Configuration Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if appsettings.json exists
$appSettingsPath = "../Dragonscale_Storyteller/appsettings.json"
if (-not (Test-Path $appSettingsPath)) {
    Write-Host "❌ ERROR: appsettings.json not found at $appSettingsPath" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Found appsettings.json" -ForegroundColor Green

# Read and parse JSON
try {
    $config = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
} catch {
    Write-Host "❌ ERROR: Failed to parse appsettings.json" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "✓ Successfully parsed configuration" -ForegroundColor Green
Write-Host ""

# Verify NebiusAi section exists
if (-not $config.NebiusAi) {
    Write-Host "❌ ERROR: NebiusAi section not found in configuration" -ForegroundColor Red
    exit 1
}

Write-Host "Checking Nebius AI Configuration:" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Yellow
Write-Host ""

# Check API Key
Write-Host "API Key:" -NoNewline
if ([string]::IsNullOrWhiteSpace($config.NebiusAi.ApiKey)) {
    Write-Host " ⚠ NOT CONFIGURED" -ForegroundColor Yellow
    Write-Host "  Please add your Nebius AI API key to appsettings.json" -ForegroundColor Yellow
    $apiKeyConfigured = $false
} else {
    $keyLength = $config.NebiusAi.ApiKey.Length
    $maskedKey = $config.NebiusAi.ApiKey.Substring(0, [Math]::Min(8, $keyLength)) + "..." + 
                 $config.NebiusAi.ApiKey.Substring([Math]::Max(0, $keyLength - 4))
    Write-Host " ✓ CONFIGURED ($maskedKey)" -ForegroundColor Green
    $apiKeyConfigured = $true
}

# Check Base URL
Write-Host "Base URL:" -NoNewline
if ([string]::IsNullOrWhiteSpace($config.NebiusAi.BaseUrl)) {
    Write-Host " ❌ NOT CONFIGURED" -ForegroundColor Red
    $baseUrlValid = $false
} else {
    Write-Host " $($config.NebiusAi.BaseUrl)" -ForegroundColor Cyan
    if ($config.NebiusAi.BaseUrl -match "nebius") {
        Write-Host "  ✓ URL contains 'nebius'" -ForegroundColor Green
        $baseUrlValid = $true
    } else {
        Write-Host "  ⚠ URL does not contain 'nebius'" -ForegroundColor Yellow
        $baseUrlValid = $false
    }
}

# Check Text Model
Write-Host "Text Model:" -NoNewline
$expectedTextModel = "meta-llama/Meta-Llama-3.1-8B-Instruct-fast"
if ($config.NebiusAi.TextModel -eq $expectedTextModel) {
    Write-Host " ✓ $($config.NebiusAi.TextModel)" -ForegroundColor Green
    $textModelValid = $true
} else {
    Write-Host " ❌ $($config.NebiusAi.TextModel)" -ForegroundColor Red
    Write-Host "  Expected: $expectedTextModel" -ForegroundColor Yellow
    $textModelValid = $false
}

# Check Image Model
Write-Host "Image Model:" -NoNewline
$expectedImageModel = "black-forest-labs/flux-schnell"
if ($config.NebiusAi.ImageModel -eq $expectedImageModel) {
    Write-Host " ✓ $($config.NebiusAi.ImageModel)" -ForegroundColor Green
    $imageModelValid = $true
} else {
    Write-Host " ❌ $($config.NebiusAi.ImageModel)" -ForegroundColor Red
    Write-Host "  Expected: $expectedImageModel" -ForegroundColor Yellow
    $imageModelValid = $false
}

Write-Host ""
Write-Host "=================================" -ForegroundColor Yellow
Write-Host ""

# Summary
$allValid = $baseUrlValid -and $textModelValid -and $imageModelValid

if ($allValid) {
    Write-Host "✓ Configuration is VALID" -ForegroundColor Green
    if ($apiKeyConfigured) {
        Write-Host "✓ API Key is configured - integration tests can run" -ForegroundColor Green
    } else {
        Write-Host "⚠ API Key not configured - integration tests will be skipped" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Configuration has ERRORS - please fix the issues above" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Model Information:" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "Text Model: Meta-Llama-3.1-8B-Instruct-fast" -ForegroundColor White
Write-Host "  - Fast inference optimized version" -ForegroundColor Gray
Write-Host "  - Used for content analysis and story generation" -ForegroundColor Gray
Write-Host ""
Write-Host "Image Model: flux-schnell" -ForegroundColor White
Write-Host "  - Fast image generation model" -ForegroundColor Gray
Write-Host "  - Used for creating image prompts" -ForegroundColor Gray
Write-Host ""

# Check if test configuration exists
$testConfigPath = "./appsettings.json"
if (Test-Path $testConfigPath) {
    Write-Host "✓ Test configuration file exists" -ForegroundColor Green
} else {
    Write-Host "⚠ Test configuration file not found - creating from main config..." -ForegroundColor Yellow
    Copy-Item $appSettingsPath $testConfigPath
    Write-Host "✓ Test configuration created" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verification Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
