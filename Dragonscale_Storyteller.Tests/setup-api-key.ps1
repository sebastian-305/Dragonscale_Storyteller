# Setup API Key for Integration Tests
# This script helps configure the Nebius AI API key using User Secrets

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Nebius AI API Key Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the test project directory
if (-not (Test-Path "Dragonscale_Storyteller.Tests.csproj")) {
    Write-Host "❌ ERROR: This script must be run from the Dragonscale_Storyteller.Tests directory" -ForegroundColor Red
    Write-Host "Current directory: $(Get-Location)" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Found test project" -ForegroundColor Green
Write-Host ""

# Check if user secrets are initialized
Write-Host "Checking User Secrets configuration..." -ForegroundColor Yellow

$userSecretsId = dotnet user-secrets list 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠ User Secrets not initialized. Initializing now..." -ForegroundColor Yellow
    dotnet user-secrets init
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERROR: Failed to initialize User Secrets" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ User Secrets initialized" -ForegroundColor Green
} else {
    Write-Host "✓ User Secrets already initialized" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Prompt for API key
Write-Host "Please enter your Nebius AI API key:" -ForegroundColor Yellow
Write-Host "(The key will not be displayed as you type)" -ForegroundColor Gray
Write-Host ""

$apiKey = Read-Host -AsSecureString "API Key"
$apiKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey)
)

if ([string]::IsNullOrWhiteSpace($apiKeyPlain)) {
    Write-Host ""
    Write-Host "❌ ERROR: API key cannot be empty" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Setting API key in User Secrets..." -ForegroundColor Yellow

# Set the API key
dotnet user-secrets set "NebiusAi:ApiKey" $apiKeyPlain
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Failed to set API key" -ForegroundColor Red
    exit 1
}

Write-Host "✓ API key configured successfully" -ForegroundColor Green
Write-Host ""

# Verify the configuration
Write-Host "Verifying configuration..." -ForegroundColor Yellow
Write-Host ""

$secrets = dotnet user-secrets list
if ($secrets -match "NebiusAi:ApiKey") {
    Write-Host "✓ API key found in User Secrets" -ForegroundColor Green
    
    # Mask the key for display
    $keyLength = $apiKeyPlain.Length
    $maskedKey = $apiKeyPlain.Substring(0, [Math]::Min(8, $keyLength)) + "..." + 
                 $apiKeyPlain.Substring([Math]::Max(0, $keyLength - 4))
    Write-Host "  Key: $maskedKey" -ForegroundColor Gray
} else {
    Write-Host "⚠ WARNING: API key not found in User Secrets list" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Run verification script
Write-Host "Running configuration verification..." -ForegroundColor Yellow
Write-Host ""

if (Test-Path "./verify-nebius-config.ps1") {
    & ./verify-nebius-config.ps1
} else {
    Write-Host "⚠ Verification script not found, skipping..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Run configuration tests:" -ForegroundColor White
Write-Host "   dotnet test --filter 'FullyQualifiedName~Configuration'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Run all integration tests:" -ForegroundColor White
Write-Host "   dotnet test --filter 'FullyQualifiedName~Integration'" -ForegroundColor Gray
Write-Host ""
Write-Host "3. View detailed test output:" -ForegroundColor White
Write-Host "   dotnet test --filter 'FullyQualifiedName~Integration' --logger 'console;verbosity=detailed'" -ForegroundColor Gray
Write-Host ""

# Offer to run configuration tests
Write-Host "Would you like to run configuration tests now? (Y/N)" -ForegroundColor Yellow
$response = Read-Host

if ($response -eq 'Y' -or $response -eq 'y') {
    Write-Host ""
    Write-Host "Running configuration tests..." -ForegroundColor Cyan
    Write-Host ""
    dotnet test --filter "FullyQualifiedName~Configuration" --logger "console;verbosity=normal"
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
