using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Dragonscale_Storyteller.Services;
using Dragonscale_Storyteller.Configuration;
using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Exceptions;

namespace Dragonscale_Storyteller.Tests.Integration;

/// <summary>
/// Integration tests specifically for Nebius AI service
/// Tests: Task 10.2 - Verify Nebius AI integration
/// </summary>
public class NebiusAiIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IAiService _aiService;
    private readonly NebiusAiConfiguration _config;

    public NebiusAiIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Build configuration - supports appsettings.json, User Secrets, and Environment Variables
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<NebiusAiIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup DI container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure Nebius AI
        services.Configure<NebiusAiConfiguration>(configuration.GetSection("NebiusAi"));
        services.AddScoped<IAiService, NebiusAiService>();

        _serviceProvider = services.BuildServiceProvider();
        _aiService = _serviceProvider.GetRequiredService<IAiService>();
        _config = _serviceProvider.GetRequiredService<IOptions<NebiusAiConfiguration>>().Value;
    }

    [Fact]
    public void Configuration_ModelNames_AreCorrect()
    {
        // Arrange & Act
        _output.WriteLine("Verifying Nebius AI configuration...");
        _output.WriteLine($"Text Model: {_config.TextModel}");
        _output.WriteLine($"Image Model: {_config.ImageModel}");
        _output.WriteLine($"Base URL: {_config.BaseUrl}");

        // Assert
        Assert.Equal("meta-llama/Meta-Llama-3.1-8B-Instruct-fast", _config.TextModel);
        Assert.Equal("black-forest-labs/flux-schnell", _config.ImageModel);
        Assert.Contains("nebius", _config.BaseUrl.ToLower());
        
        _output.WriteLine("✓ Model names are correctly configured");
    }

    [Fact]
    public void Configuration_BaseUrl_IsValid()
    {
        // Arrange & Act
        _output.WriteLine($"Checking Base URL: {_config.BaseUrl}");

        // Assert
        Assert.NotNull(_config.BaseUrl);
        Assert.NotEmpty(_config.BaseUrl);
        Assert.True(Uri.TryCreate(_config.BaseUrl, UriKind.Absolute, out var uri));
        Assert.Equal("https", uri.Scheme);
        
        _output.WriteLine($"✓ Base URL is valid: {uri}");
    }

    [Fact]
    public void Configuration_ApiKey_IsConfigured()
    {
        // Arrange & Act
        _output.WriteLine("Checking API key configuration...");
        var hasApiKey = !string.IsNullOrWhiteSpace(_config.ApiKey);

        // Assert
        if (hasApiKey)
        {
            _output.WriteLine($"✓ API key is configured (length: {_config.ApiKey.Length})");
            Assert.True(_config.ApiKey.Length > 10, "API key seems too short");
        }
        else
        {
            _output.WriteLine("⚠ API key is not configured - live tests will be skipped");
        }
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task ContentAnalysis_TechnicalDocument_ReturnsValidAnalysis()
    {
        // Arrange
        _output.WriteLine("Test: Content Analysis - Technical Document");
        var technicalText = @"
            TECHNICAL MANUAL - XR-2000 Coffee Maker
            
            SAFETY INSTRUCTIONS:
            1. Always unplug before cleaning
            2. Do not immerse in water
            3. Keep away from children
            
            OPERATION:
            1. Fill water reservoir with fresh water
            2. Add coffee grounds to filter basket
            3. Press power button to start brewing
            4. Wait for brewing cycle to complete (approximately 5 minutes)
            
            TROUBLESHOOTING:
            Problem: Machine won't start
            Solution: Check power connection and ensure water reservoir is filled
            
            Problem: Coffee tastes weak
            Solution: Use more coffee grounds or adjust grind size
        ";

        // Act
        _output.WriteLine("Analyzing technical document...");
        var startTime = DateTime.UtcNow;
        var result = await _aiService.AnalyzeContentAsync(technicalText);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        _output.WriteLine($"Analysis completed in {duration.TotalMilliseconds}ms");
        Assert.NotNull(result);
        Assert.NotNull(result.KeyFacts);
        Assert.NotNull(result.Entities);
        Assert.NotNull(result.Concepts);
        Assert.NotEmpty(result.OverallContext);
        Assert.NotEmpty(result.SourceType);

        _output.WriteLine($"Source Type: {result.SourceType}");
        _output.WriteLine($"Key Facts: {result.KeyFacts.Count}");
        _output.WriteLine($"Entities: {result.Entities.Count}");
        _output.WriteLine($"Concepts: {result.Concepts.Count}");
        _output.WriteLine($"Overall Context: {result.OverallContext}");

        // Verify quality
        Assert.True(result.KeyFacts.Count > 0, "Should extract key facts");
        Assert.True(result.Entities.Count > 0, "Should identify entities");
        Assert.True(result.Concepts.Count > 0, "Should identify concepts");
        
        _output.WriteLine("✓ Content analysis successful");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task ContentAnalysis_NewsArticle_ReturnsValidAnalysis()
    {
        // Arrange
        _output.WriteLine("Test: Content Analysis - News Article");
        var newsText = @"
            LOCAL NEWS
            
            City Park Renovation Complete
            By Jane Reporter
            
            After six months of construction, the Central Park renovation project 
            has been completed. The park now features new playground equipment, 
            walking paths, and a community garden.
            
            Mayor Smith stated: 'This park will serve our community for generations 
            to come.' The grand opening ceremony is scheduled for next Saturday at 10 AM.
            
            The $2 million project was funded through a combination of city budget 
            allocations and community donations.
        ";

        // Act
        _output.WriteLine("Analyzing news article...");
        var result = await _aiService.AnalyzeContentAsync(newsText);

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Source Type: {result.SourceType}");
        _output.WriteLine($"Entities found: {string.Join(", ", result.Entities)}");
        
        var sourceTypeLower = result.SourceType.ToLower();
        Assert.True(sourceTypeLower.Contains("article") || sourceTypeLower.Contains("news"));
        
        _output.WriteLine("✓ News article analysis successful");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task ContentAnalysis_ShoppingList_ReturnsValidAnalysis()
    {
        // Arrange
        _output.WriteLine("Test: Content Analysis - Shopping List");
        var listText = @"
            SHOPPING LIST
            
            Groceries:
            - Milk (2 liters)
            - Bread (whole wheat)
            - Eggs (dozen)
            - Apples (6)
            - Chicken breast (1 kg)
            - Pasta
            - Tomato sauce
            - Cheese
            
            Household:
            - Paper towels
            - Dish soap
            - Laundry detergent
        ";

        // Act
        _output.WriteLine("Analyzing shopping list...");
        var result = await _aiService.AnalyzeContentAsync(listText);

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Source Type: {result.SourceType}");
        _output.WriteLine($"Items identified: {result.Entities.Count}");
        
        Assert.True(result.Entities.Count > 0, "Should identify items from list");
        
        _output.WriteLine("✓ Shopping list analysis successful");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task StoryGeneration_FromAnalysis_CreatesCompleteStory()
    {
        // Arrange
        _output.WriteLine("Test: Story Generation from Analysis");
        var analysis = new ContentAnalysisResult
        {
            KeyFacts = new List<string>
            {
                "Coffee maker model XR-2000",
                "Requires water and coffee grounds",
                "Brewing takes 5 minutes",
                "Has safety features"
            },
            Entities = new List<string>
            {
                "XR-2000 Coffee Maker",
                "water reservoir",
                "filter basket",
                "power button"
            },
            Concepts = new List<string>
            {
                "brewing",
                "safety",
                "operation",
                "troubleshooting"
            },
            OverallContext = "Technical manual for a coffee making appliance",
            SourceType = "manual"
        };

        // Act
        _output.WriteLine("Generating story from analysis...");
        var startTime = DateTime.UtcNow;
        var result = await _aiService.GenerateStoryAsync(analysis);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        _output.WriteLine($"Story generation completed in {duration.TotalMilliseconds}ms");
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
        Assert.NotNull(result.Phases);
        Assert.Equal(4, result.Phases.Count);

        _output.WriteLine($"Story Title: {result.Title}");
        
        // Verify each phase
        var expectedPhases = new[] { "Introduction", "Conflict", "Climax", "Resolution" };
        for (int i = 0; i < result.Phases.Count; i++)
        {
            var phase = result.Phases[i];
            _output.WriteLine($"\nPhase {i + 1}: {phase.Name}");
            _output.WriteLine($"  Mood: {phase.Mood}");
            _output.WriteLine($"  Summary: {phase.Summary[..Math.Min(100, phase.Summary.Length)]}...");

            Assert.NotEmpty(phase.Name);
            Assert.NotEmpty(phase.Summary);
            Assert.NotEmpty(phase.Mood);
            Assert.True(phase.Summary.Length >= 50, "Summary should be detailed (at least 50 characters)");
        }

        _output.WriteLine("\n✓ Story generation successful with all 4 phases");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task ImagePromptGeneration_ForPhase_CreatesDetailedPrompt()
    {
        // Arrange
        _output.WriteLine("Test: Image Prompt Generation");
        var phase = new StoryPhase
        {
            Name = "The Awakening",
            Summary = "In a dimly lit kitchen, the ancient coffee maker begins to hum with mysterious energy. Steam rises from its reservoir as it prepares to brew its first magical cup.",
            Mood = "mysterious",
            Order = 1
        };

        // Act
        _output.WriteLine($"Generating image prompt for phase: {phase.Name}");
        var startTime = DateTime.UtcNow;
        var prompt = await _aiService.GenerateImagePromptAsync(phase);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        _output.WriteLine($"Prompt generation completed in {duration.TotalMilliseconds}ms");
        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt);
        Assert.True(prompt.Length >= 50, "Prompt should be detailed");

        _output.WriteLine($"\nGenerated Prompt ({prompt.Length} characters):");
        _output.WriteLine(prompt);

        // Verify prompt quality - should contain descriptive elements
        var hasVisualElements = prompt.ToLower().Contains("light") || 
                               prompt.ToLower().Contains("dark") ||
                               prompt.ToLower().Contains("color") ||
                               prompt.ToLower().Contains("atmosphere");
        
        Assert.True(hasVisualElements, "Prompt should contain visual descriptive elements");
        
        _output.WriteLine("\n✓ Image prompt generation successful");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task ImagePromptGeneration_DifferentMoods_ProducesVariedPrompts()
    {
        // Arrange
        _output.WriteLine("Test: Image Prompt Variation Across Moods");
        var phases = new[]
        {
            new StoryPhase { Name = "Phase 1", Summary = "A peaceful beginning", Mood = "calm", Order = 1 },
            new StoryPhase { Name = "Phase 2", Summary = "Tension rises", Mood = "tense", Order = 2 },
            new StoryPhase { Name = "Phase 3", Summary = "The dramatic peak", Mood = "dramatic", Order = 3 },
            new StoryPhase { Name = "Phase 4", Summary = "Victory achieved", Mood = "triumphant", Order = 4 }
        };

        // Act & Assert
        var prompts = new List<string>();
        foreach (var phase in phases)
        {
            _output.WriteLine($"\nGenerating prompt for {phase.Mood} mood...");
            var prompt = await _aiService.GenerateImagePromptAsync(phase);
            prompts.Add(prompt);
            
            _output.WriteLine($"Prompt: {prompt[..Math.Min(150, prompt.Length)]}...");
            Assert.NotEmpty(prompt);
        }

        // Verify prompts are different
        var uniquePrompts = prompts.Distinct().Count();
        Assert.Equal(4, uniquePrompts);
        
        _output.WriteLine("\n✓ All prompts are unique and mood-appropriate");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task ErrorHandling_InvalidApiKey_ThrowsAuthenticationException()
    {
        // Arrange
        _output.WriteLine("Test: Error Handling - Invalid API Key");
        
        // Create service with invalid API key
        var invalidConfig = new NebiusAiConfiguration
        {
            ApiKey = "invalid-key-12345",
            BaseUrl = _config.BaseUrl,
            TextModel = _config.TextModel,
            ImageModel = _config.ImageModel
        };

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(Options.Create(invalidConfig));
        services.AddScoped<IAiService, NebiusAiService>();
        
        var provider = services.BuildServiceProvider();
        var invalidAiService = provider.GetRequiredService<IAiService>();

        // Act & Assert
        _output.WriteLine("Attempting content analysis with invalid API key...");
        var exception = await Assert.ThrowsAsync<AiServiceException>(async () =>
        {
            await invalidAiService.AnalyzeContentAsync("Test content");
        });

        _output.WriteLine($"Exception caught: {exception.ErrorType}");
        Assert.Equal(AiServiceErrorType.AuthenticationFailed, exception.ErrorType);
        
        _output.WriteLine("✓ Authentication error handled correctly");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task Performance_MultipleRequests_CompletesInReasonableTime()
    {
        // Arrange
        _output.WriteLine("Test: Performance - Multiple Sequential Requests");
        var testText = "This is a test document for performance testing.";
        var requestCount = 3;

        // Act
        _output.WriteLine($"Making {requestCount} sequential requests...");
        var startTime = DateTime.UtcNow;
        var results = new List<ContentAnalysisResult>();

        for (int i = 0; i < requestCount; i++)
        {
            _output.WriteLine($"Request {i + 1}/{requestCount}...");
            var result = await _aiService.AnalyzeContentAsync(testText);
            results.Add(result);
        }

        var totalDuration = DateTime.UtcNow - startTime;
        var averageDuration = totalDuration.TotalMilliseconds / requestCount;

        // Assert
        _output.WriteLine($"\nTotal time: {totalDuration.TotalMilliseconds}ms");
        _output.WriteLine($"Average time per request: {averageDuration}ms");
        
        Assert.Equal(requestCount, results.Count);
        Assert.All(results, r => Assert.NotNull(r));
        
        // Performance expectation: average should be under 10 seconds per request
        Assert.True(averageDuration < 10000, $"Average request time ({averageDuration}ms) exceeds 10 seconds");
        
        _output.WriteLine("✓ Performance test passed");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task Integration_CompleteWorkflow_AllStepsSucceed()
    {
        // Arrange
        _output.WriteLine("Test: Complete AI Workflow Integration");
        var sourceText = @"
            USER GUIDE - Smart Home Hub
            
            The Smart Home Hub connects all your devices in one place.
            Control lights, thermostats, and security cameras from your phone.
            
            Setup is simple:
            1. Plug in the hub
            2. Download the app
            3. Follow the on-screen instructions
            
            Features:
            - Voice control
            - Automated schedules
            - Energy monitoring
            - Remote access
        ";

        // Act - Step 1: Content Analysis
        _output.WriteLine("\nStep 1: Analyzing content...");
        var analysis = await _aiService.AnalyzeContentAsync(sourceText);
        Assert.NotNull(analysis);
        _output.WriteLine($"✓ Analysis complete: {analysis.Entities.Count} entities, {analysis.Concepts.Count} concepts");

        // Act - Step 2: Story Generation
        _output.WriteLine("\nStep 2: Generating story...");
        var story = await _aiService.GenerateStoryAsync(analysis);
        Assert.NotNull(story);
        Assert.Equal(4, story.Phases.Count);
        _output.WriteLine($"✓ Story generated: {story.Title}");

        // Act - Step 3: Image Prompt Generation
        _output.WriteLine("\nStep 3: Generating image prompts...");
        var prompts = new List<string>();
        foreach (var phase in story.Phases)
        {
            var storyPhase = new StoryPhase
            {
                Name = phase.Name,
                Summary = phase.Summary,
                Mood = phase.Mood,
                Order = story.Phases.IndexOf(phase) + 1
            };
            
            var prompt = await _aiService.GenerateImagePromptAsync(storyPhase);
            prompts.Add(prompt);
            _output.WriteLine($"✓ Prompt for {phase.Name}: {prompt.Length} characters");
        }

        // Assert
        Assert.Equal(4, prompts.Count);
        Assert.All(prompts, p => Assert.NotEmpty(p));
        
        _output.WriteLine("\n✓ Complete workflow integration successful!");
        _output.WriteLine($"   - Content analyzed");
        _output.WriteLine($"   - Story created with {story.Phases.Count} phases");
        _output.WriteLine($"   - {prompts.Count} image prompts generated");
    }
}
