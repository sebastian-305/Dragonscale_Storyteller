# API Key Configuration Guide

This guide explains how to configure your Nebius AI API key for running integration tests.

## Configuration Priority

The tests load configuration in this order (later sources override earlier ones):

1. `appsettings.json` (lowest priority)
2. User Secrets
3. Environment Variables (highest priority)

## Option 1: User Secrets (Recommended for Development)

User Secrets is the recommended approach for local development as it keeps sensitive data out of source control.

### Setup User Secrets

```bash
# Navigate to the test project directory
cd Dragonscale_Storyteller.Tests

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set the Nebius AI API key
dotnet user-secrets set "NebiusAi:ApiKey" "your-api-key-here"

# Verify the secret was set
dotnet user-secrets list
```

### Expected Output
```
NebiusAi:ApiKey = your-api-key-here
```

### Where are User Secrets Stored?

User Secrets are stored outside your project directory:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

## Option 2: Environment Variables

Environment variables are useful for CI/CD pipelines and temporary testing.

### Windows (PowerShell)

```powershell
# Set for current session
$env:NebiusAi__ApiKey = "your-api-key-here"

# Set permanently for user
[System.Environment]::SetEnvironmentVariable("NebiusAi__ApiKey", "your-api-key-here", "User")

# Verify
$env:NebiusAi__ApiKey
```

### Windows (Command Prompt)

```cmd
# Set for current session
set NebiusAi__ApiKey=your-api-key-here

# Set permanently
setx NebiusAi__ApiKey "your-api-key-here"
```

### Linux/macOS (Bash)

```bash
# Set for current session
export NebiusAi__ApiKey="your-api-key-here"

# Set permanently (add to ~/.bashrc or ~/.zshrc)
echo 'export NebiusAi__ApiKey="your-api-key-here"' >> ~/.bashrc
source ~/.bashrc

# Verify
echo $NebiusAi__ApiKey
```

**Note**: Use double underscores (`__`) in environment variable names to represent nested configuration sections.

## Option 3: appsettings.json (Not Recommended)

⚠️ **Warning**: Only use this for testing. Never commit API keys to source control!

Edit `Dragonscale_Storyteller.Tests/appsettings.json`:

```json
{
  "NebiusAi": {
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://api.studio.nebius.ai/v1/",
    "TextModel": "meta-llama/Meta-Llama-3.1-8B-Instruct-fast",
    "ImageModel": "black-forest-labs/flux-schnell"
  }
}
```

Make sure this file is in `.gitignore` if you add the API key!

## Verifying Configuration

### Method 1: Run the Verification Script

```powershell
cd Dragonscale_Storyteller.Tests
./verify-nebius-config.ps1
```

This will show:
- ✓ API Key is configured (if found)
- ⚠ API Key not configured (if missing)

### Method 2: Run Configuration Tests

```bash
# Run only configuration verification tests (no API calls)
dotnet test --filter "FullyQualifiedName~Configuration"
```

These tests check:
- Model names are correct
- Base URL is valid
- API key is configured

## Running Tests with API Key

Once configured, remove the `Skip` attribute from tests or run all tests:

```bash
# Run all integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run with verbose output
dotnet test --filter "FullyQualifiedName~Integration" --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~NebiusAiIntegrationTests"
```

## Troubleshooting

### Issue: "API Key not configured"

**Solution**: Verify configuration is loaded correctly

```bash
# Check User Secrets
dotnet user-secrets list

# Check Environment Variable (PowerShell)
$env:NebiusAi__ApiKey

# Check Environment Variable (Bash)
echo $NebiusAi__ApiKey
```

### Issue: "Authentication Failed"

**Possible Causes**:
1. API key is incorrect
2. API key has expired
3. API key doesn't have required permissions

**Solution**: 
- Verify your API key in the Nebius AI console
- Generate a new API key if needed
- Update the configuration with the new key

### Issue: Tests are skipped

**Cause**: Tests have `Skip` attribute by default

**Solution**: 
1. Edit test files
2. Remove or comment out the `Skip` parameter:
   ```csharp
   // Before
   [Fact(Skip = "Requires valid Nebius AI API key")]
   
   // After
   [Fact]
   ```

## CI/CD Configuration

### GitHub Actions

```yaml
- name: Run Integration Tests
  env:
    NebiusAi__ApiKey: ${{ secrets.NEBIUS_API_KEY }}
  run: dotnet test --filter "FullyQualifiedName~Integration"
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    arguments: '--filter "FullyQualifiedName~Integration"'
  env:
    NebiusAi__ApiKey: $(NebiusApiKey)
```

### GitLab CI

```yaml
test:
  script:
    - dotnet test --filter "FullyQualifiedName~Integration"
  variables:
    NebiusAi__ApiKey: $NEBIUS_API_KEY
```

## Security Best Practices

✅ **DO**:
- Use User Secrets for local development
- Use environment variables in CI/CD
- Use secret management services in production
- Rotate API keys regularly
- Limit API key permissions to minimum required

❌ **DON'T**:
- Commit API keys to source control
- Share API keys in plain text
- Use production keys for testing
- Log API keys in application logs
- Store API keys in unsecured locations

## Quick Start

For the fastest setup:

```bash
# 1. Navigate to test project
cd Dragonscale_Storyteller.Tests

# 2. Set API key using User Secrets
dotnet user-secrets set "NebiusAi:ApiKey" "your-api-key-here"

# 3. Verify configuration
./verify-nebius-config.ps1

# 4. Run tests
dotnet test --filter "FullyQualifiedName~Configuration"
```

If configuration tests pass, you're ready to run the full integration test suite!

## Additional Resources

- [.NET User Secrets Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [.NET Configuration Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Nebius AI Documentation](https://nebius.ai/docs)

---

**Last Updated**: November 16, 2025  
**Applies To**: Dragonscale Storyteller Integration Tests
