using Dragonscale_Storyteller.Configuration;
using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Dragonscale_Storyteller.Tests.Services;

public class NebiusAiServiceTests
{
    private readonly Mock<ILogger<NebiusAiService>> _loggerMock;
    private readonly NebiusAiConfiguration _config;

    public NebiusAiServiceTests()
    {
        _loggerMock = new Mock<ILogger<NebiusAiService>>();
        _config = new NebiusAiConfiguration
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.studio.nebius.ai/v1/",
            TextModel = "meta-llama/Meta-Llama-3.1-8B-Instruct-fast",
            ImageModel = "black-forest-labs/flux-schnell"
        };
    }

    [Fact]
    public void Constructor_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new NebiusAiConfiguration
        {
            ApiKey = "",
            BaseUrl = "https://api.studio.nebius.ai/v1/",
            TextModel = "meta-llama/Meta-Llama-3.1-8B-Instruct-fast"
        };
        var options = Options.Create(invalidConfig);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new NebiusAiService(options, _loggerMock.Object));
        
        exception.Message.Should().Contain("API key is not configured");
    }

    [Fact]
    public void Constructor_WithMissingBaseUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new NebiusAiConfiguration
        {
            ApiKey = "test-key",
            BaseUrl = "",
            TextModel = "meta-llama/Meta-Llama-3.1-8B-Instruct-fast"
        };
        var options = Options.Create(invalidConfig);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new NebiusAiService(options, _loggerMock.Object));
        
        exception.Message.Should().Contain("base URL is not configured");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_CreatesInstance()
    {
        // Arrange
        var options = Options.Create(_config);

        // Act
        var service = new NebiusAiService(options, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeContentAsync_WithNullText_ThrowsException()
    {
        // Arrange
        var options = Options.Create(_config);
        var service = new NebiusAiService(options, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            service.AnalyzeContentAsync(null!));
    }



    [Fact]
    public async Task GenerateImagePromptAsync_WithNullPhase_ThrowsException()
    {
        // Arrange
        var options = Options.Create(_config);
        var service = new NebiusAiService(options, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            service.GenerateImagePromptAsync(null!));
    }

    [Fact]
    public void AnalyzeContentAsync_ValidatesContentAnalysisResultStructure()
    {
        // This test verifies that ContentAnalysisResult has the expected properties
        var result = new ContentAnalysisResult
        {
            KeyFacts = new List<string> { "fact1", "fact2" },
            Entities = new List<string> { "entity1" },
            Concepts = new List<string> { "concept1" },
            OverallContext = "test context",
            SourceType = "manual"
        };

        result.KeyFacts.Should().HaveCount(2);
        result.Entities.Should().HaveCount(1);
        result.Concepts.Should().HaveCount(1);
        result.OverallContext.Should().Be("test context");
        result.SourceType.Should().Be("manual");
    }

    [Fact]
    public void GenerateStoryAsync_ValidatesStoryGenerationResultStructure()
    {
        // This test verifies that StoryGenerationResult has the expected properties
        var result = new StoryGenerationResult
        {
            Title = "Test Story",
            Phases = new List<StoryPhaseData>
            {
                new StoryPhaseData
                {
                    Name = "Introduction",
                    Summary = "Test summary",
                    Mood = "mysterious"
                }
            }
        };

        result.Title.Should().Be("Test Story");
        result.Phases.Should().HaveCount(1);
        result.Phases[0].Name.Should().Be("Introduction");
        result.Phases[0].Summary.Should().Be("Test summary");
        result.Phases[0].Mood.Should().Be("mysterious");
    }

    [Fact]
    public void GenerateImagePromptAsync_ValidatesStoryPhaseStructure()
    {
        // This test verifies that StoryPhase has the expected properties
        var phase = new StoryPhase
        {
            Name = "Introduction",
            Summary = "Test summary",
            Mood = "mysterious",
            ImagePrompt = "Test prompt",
            Order = 1
        };

        phase.Name.Should().Be("Introduction");
        phase.Summary.Should().Be("Test summary");
        phase.Mood.Should().Be("mysterious");
        phase.ImagePrompt.Should().Be("Test prompt");
        phase.Order.Should().Be(1);
    }
}
