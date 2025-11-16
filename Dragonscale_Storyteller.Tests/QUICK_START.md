# Quick Start Guide - Running Integration Tests

## 🚀 Fast Setup (3 Steps)

### Step 1: Configure API Key

```powershell
cd Dragonscale_Storyteller.Tests
./setup-api-key.ps1
```

The script will:
- Initialize User Secrets if needed
- Prompt for your Nebius AI API key (input is hidden)
- Store it securely in User Secrets
- Verify the configuration

### Step 2: Verify Configuration

```powershell
./verify-nebius-config.ps1
```

Expected output:
```
✓ Base URL: https://api.studio.nebius.ai/v1/
✓ Text Model: meta-llama/Meta-Llama-3.1-8B-Instruct-fast
✓ Image Model: black-forest-labs/flux-schnell
✓ API Key is configured
```

### Step 3: Run Tests

```bash
# Run configuration tests (no API calls, fast)
dotnet test --filter "FullyQualifiedName~Configuration"

# Run all integration tests (makes API calls)
dotnet test --filter "FullyQualifiedName~Integration"
```

---

## 📋 Manual Configuration

If you prefer to configure manually:

```bash
# Set API key using User Secrets
dotnet user-secrets set "NebiusAi:ApiKey" "your-api-key-here"

# Verify it was set
dotnet user-secrets list
```

---

## 🧪 Running Specific Tests

```bash
# Configuration tests only (no API key needed)
dotnet test --filter "FullyQualifiedName~Configuration"

# End-to-end workflow tests
dotnet test --filter "FullyQualifiedName~EndToEndIntegrationTests"

# Nebius AI integration tests
dotnet test --filter "FullyQualifiedName~NebiusAiIntegrationTests"

# Single test method
dotnet test --filter "FullyQualifiedName~Configuration_ModelNames_AreCorrect"

# With detailed output
dotnet test --filter "FullyQualifiedName~Integration" --logger "console;verbosity=detailed"
```

---

## 📖 Test Categories

### Configuration Tests (No API Key Required)
- ✅ Model name verification
- ✅ Base URL validation
- ✅ Configuration structure check

### Integration Tests (API Key Required)
- 🔄 Content analysis (technical docs, articles, lists)
- 🔄 Story generation
- 🔄 Image prompt generation
- 🔄 Error handling
- 🔄 Performance testing
- 🔄 End-to-end workflow

---

## 🔧 Troubleshooting

### API Key Not Found

```bash
# Check if User Secrets are configured
dotnet user-secrets list

# If empty, run setup script
./setup-api-key.ps1
```

### Tests Are Skipped

Tests have `Skip` attribute by default. To run them:

1. **Option A**: Remove skip attribute from test files
2. **Option B**: Tests will run automatically if API key is configured

### Authentication Failed

```bash
# Verify your API key is correct
dotnet user-secrets list

# Update if needed
dotnet user-secrets set "NebiusAi:ApiKey" "new-api-key"
```

---

## 📚 Documentation

- **Full Configuration Guide**: `API_KEY_CONFIGURATION.md`
- **Manual Testing Guide**: `MANUAL_TESTING_GUIDE.md`
- **Integration Test Summary**: `INTEGRATION_TEST_SUMMARY.md`
- **Complete Testing Documentation**: `TESTING_COMPLETE.md`

---

## ⚡ One-Liner Commands

```bash
# Complete setup and run all tests
cd Dragonscale_Storyteller.Tests && ./setup-api-key.ps1 && dotnet test --filter "FullyQualifiedName~Integration"

# Quick verification
./verify-nebius-config.ps1 && dotnet test --filter "FullyQualifiedName~Configuration"

# Run tests with detailed output
dotnet test --filter "FullyQualifiedName~Integration" -v detailed
```

---

## 🎯 Test Execution Time

| Test Suite | Test Count | Estimated Time |
|------------|-----------|----------------|
| Configuration | 3 | < 1 second |
| End-to-End | 6 | 2-5 minutes |
| Nebius AI | 12 | 3-8 minutes |
| **Total** | **21** | **5-13 minutes** |

*Times vary based on API response times and network latency*

---

## ✅ Success Indicators

After running tests, you should see:

```
Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21
```

If tests are skipped:
- API key might not be configured
- Tests might have `Skip` attribute

If tests fail:
- Check API key is valid
- Verify network connectivity
- Review test output for specific errors

---

## 🔐 Security Notes

- ✅ User Secrets are stored outside your project directory
- ✅ Never commit API keys to source control
- ✅ API keys are not displayed in test output
- ✅ Use different keys for testing and production

---

**Need Help?** Check the full documentation in `API_KEY_CONFIGURATION.md`
