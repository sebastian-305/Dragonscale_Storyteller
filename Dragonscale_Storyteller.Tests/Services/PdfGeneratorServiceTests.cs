using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UglyToad.PdfPig;

namespace Dragonscale_Storyteller.Tests.Services;

public class PdfGeneratorServiceTests
{
    private readonly Mock<ILogger<PdfGeneratorService>> _loggerMock;
    private readonly PdfGeneratorService _service;

    public PdfGeneratorServiceTests()
    {
        _loggerMock = new Mock<ILogger<PdfGeneratorService>>();
        _service = new PdfGeneratorService(_loggerMock.Object);
    }

    [Fact]
    public async Task GenerateStoryPdfAsync_WithValidStory_ReturnsPdfBytes()
    {
        // Arrange
        var story = CreateSampleStory();

        // Act
        var result = await _service.GenerateStoryPdfAsync(story);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateStoryPdfAsync_GeneratesValidPdfDocument()
    {
        // Arrange
        var story = CreateSampleStory();

        // Act
        var pdfBytes = await _service.GenerateStoryPdfAsync(story);

        // Assert - Verify it's a valid PDF by opening it
        using var pdfStream = new MemoryStream(pdfBytes);
        using var pdfDocument = PdfDocument.Open(pdfStream);
        
        pdfDocument.Should().NotBeNull();
        pdfDocument.NumberOfPages.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateStoryPdfAsync_IncludesStoryTitle()
    {
        // Arrange
        var story = CreateSampleStory();

        // Act
        var pdfBytes = await _service.GenerateStoryPdfAsync(story);

        // Assert
        using var pdfStream = new MemoryStream(pdfBytes);
        using var pdfDocument = PdfDocument.Open(pdfStream);
        
        var firstPage = pdfDocument.GetPage(1);
        var pageText = firstPage.Text;
        
        pageText.Should().Contain(story.Title);
    }

    [Fact]
    public async Task GenerateStoryPdfAsync_IncludesAllPhases()
    {
        // Arrange
        var story = CreateSampleStory();

        // Act
        var pdfBytes = await _service.GenerateStoryPdfAsync(story);

        // Assert
        using var pdfStream = new MemoryStream(pdfBytes);
        using var pdfDocument = PdfDocument.Open(pdfStream);
        
        var allText = string.Join(" ", pdfDocument.GetPages().Select(p => p.Text));
        
        foreach (var phase in story.Phases)
        {
            allText.Should().Contain(phase.Name);
            // Check for key words from summary and prompt (PDF text extraction may remove some spaces)
            var summaryWords = phase.Summary.Split(' ').Where(w => w.Length > 4).Take(3);
            foreach (var word in summaryWords)
            {
                allText.Should().Contain(word);
            }
            allText.Should().Contain(phase.Mood);
        }
    }

    [Fact]
    public async Task GenerateStoryPdfAsync_IncludesMetadata()
    {
        // Arrange
        var story = CreateSampleStory();

        // Act
        var pdfBytes = await _service.GenerateStoryPdfAsync(story);

        // Assert
        using var pdfStream = new MemoryStream(pdfBytes);
        using var pdfDocument = PdfDocument.Open(pdfStream);
        
        var firstPage = pdfDocument.GetPage(1);
        var pageText = firstPage.Text;
        
        pageText.Should().Contain(story.SourceFileName);
        pageText.Should().Contain(story.Id);
    }

    [Fact]
    public async Task GenerateStoryPdfAsync_WithMultiplePhases_GeneratesCorrectStructure()
    {
        // Arrange
        var story = CreateStoryWithMultiplePhases(6);

        // Act
        var pdfBytes = await _service.GenerateStoryPdfAsync(story);

        // Assert
        using var pdfStream = new MemoryStream(pdfBytes);
        using var pdfDocument = PdfDocument.Open(pdfStream);
        
        pdfDocument.NumberOfPages.Should().BeGreaterThan(0);
        
        var allText = string.Join(" ", pdfDocument.GetPages().Select(p => p.Text));
        
        // Verify all 6 phases are present
        for (int i = 0; i < 6; i++)
        {
            allText.Should().Contain($"Phase {i}");
        }
    }

    private GeneratedStory CreateSampleStory()
    {
        return new GeneratedStory
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "The Dragon's Quest",
            SourceFileName = "test-document.pdf",
            CreatedAt = DateTime.UtcNow,
            Phases = new List<StoryPhase>
            {
                new()
                {
                    Name = "Introduction",
                    Summary = "A young dragon discovers an ancient map in the mountains.",
                    Mood = "mysterious",
                    ImagePrompt = "A majestic young dragon with shimmering scales examining an old parchment map in a mountain cave, mysterious lighting, fantasy art style",
                    Order = 0
                },
                new()
                {
                    Name = "Conflict",
                    Summary = "Dark forces seek to steal the map and prevent the dragon's journey.",
                    Mood = "tense",
                    ImagePrompt = "Shadowy figures approaching a dragon in a dark forest, tense atmosphere, dramatic lighting, fantasy illustration",
                    Order = 1
                },
                new()
                {
                    Name = "Climax",
                    Summary = "An epic battle ensues as the dragon defends the ancient treasure.",
                    Mood = "dramatic",
                    ImagePrompt = "Epic battle scene with a dragon breathing fire against dark enemies, dramatic action, cinematic composition, high fantasy art",
                    Order = 2
                },
                new()
                {
                    Name = "Resolution",
                    Summary = "The dragon succeeds and discovers the true meaning of the quest.",
                    Mood = "triumphant",
                    ImagePrompt = "A victorious dragon standing on a mountain peak at sunrise, triumphant mood, golden light, inspirational fantasy scene",
                    Order = 3
                }
            }
        };
    }

    private GeneratedStory CreateStoryWithMultiplePhases(int phaseCount)
    {
        var story = CreateSampleStory();
        story.Phases.Clear();

        for (int i = 0; i < phaseCount; i++)
        {
            story.Phases.Add(new StoryPhase
            {
                Name = $"Phase {i}",
                Summary = $"This is the summary for phase {i}.",
                Mood = "neutral",
                ImagePrompt = $"Image prompt for phase {i}",
                Order = i
            });
        }

        return story;
    }
}
