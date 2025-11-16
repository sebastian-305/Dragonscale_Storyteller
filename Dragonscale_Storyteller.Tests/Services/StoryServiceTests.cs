using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Dragonscale_Storyteller.Tests.Services;

public class StoryServiceTests
{
    private readonly Mock<IPdfProcessorService> _pdfProcessorMock;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly Mock<IPdfGeneratorService> _pdfGeneratorMock;
    private readonly Mock<IStoryStorageService> _storageServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<StoryService>> _loggerMock;
    private readonly StoryService _service;

    public StoryServiceTests()
    {
        _pdfProcessorMock = new Mock<IPdfProcessorService>();
        _aiServiceMock = new Mock<IAiService>();
        _pdfGeneratorMock = new Mock<IPdfGeneratorService>();
        _storageServiceMock = new Mock<IStoryStorageService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<StoryService>>();
        
        // Setup default mock behaviors
        _pdfGeneratorMock
            .Setup(x => x.GenerateStoryPdfAsync(It.IsAny<GeneratedStory>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header
        
        _storageServiceMock
            .Setup(x => x.SaveStoryPdfAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync((string id, byte[] content) => $"generated-stories/{id}.pdf");
        
        _service = new StoryService(
            _pdfProcessorMock.Object,
            _aiServiceMock.Object,
            _pdfGeneratorMock.Object,
            _storageServiceMock.Object,
            _cache,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task CreateStoryFromPdfAsync_WithValidPdf_ReturnsGeneratedStory()
    {
        // Arrange
        var fileName = "test.pdf";
        var pdfStream = CreateSimplePdfStream("Test content");
        var extractedText = "Test content from PDF";
        
        var analysis = new ContentAnalysisResult
        {
            KeyFacts = new List<string> { "Fact 1", "Fact 2" },
            Entities = new List<string> { "Entity 1" },
            Concepts = new List<string> { "Concept 1" },
            OverallContext = "Test context",
            SourceType = "article"
        };

        var storyResult = new StoryGenerationResult
        {
            Title = "Test Story",
            Phases = new List<StoryPhaseData>
            {
                new() { Name = "Introduction", Summary = "Intro summary", Mood = "mysterious" },
                new() { Name = "Conflict", Summary = "Conflict summary", Mood = "tense" },
                new() { Name = "Climax", Summary = "Climax summary", Mood = "dramatic" },
                new() { Name = "Resolution", Summary = "Resolution summary", Mood = "triumphant" }
            }
        };

        _pdfProcessorMock
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>()))
            .ReturnsAsync(extractedText);

        _aiServiceMock
            .Setup(x => x.AnalyzeContentAsync(extractedText))
            .ReturnsAsync(analysis);

        _aiServiceMock
            .Setup(x => x.GenerateStoryAsync(analysis))
            .ReturnsAsync(storyResult);

        _aiServiceMock
            .Setup(x => x.GenerateImagePromptAsync(It.IsAny<StoryPhase>()))
            .ReturnsAsync((StoryPhase phase) => $"Image prompt for {phase.Name}");

        // Act
        var result = await _service.CreateStoryFromPdfAsync(pdfStream, fileName);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Title.Should().Be("Test Story");
        result.SourceFileName.Should().Be(fileName);
        result.Phases.Should().HaveCount(4);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify phases
        result.Phases[0].Name.Should().Be("Introduction");
        result.Phases[0].Order.Should().Be(0);
        result.Phases[0].ImagePrompt.Should().Contain("Introduction");

        result.Phases[3].Name.Should().Be("Resolution");
        result.Phases[3].Order.Should().Be(3);
        result.Phases[3].ImagePrompt.Should().Contain("Resolution");

        // Verify all services were called
        _pdfProcessorMock.Verify(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>()), Times.Once);
        _aiServiceMock.Verify(x => x.AnalyzeContentAsync(extractedText), Times.Once);
        _aiServiceMock.Verify(x => x.GenerateStoryAsync(analysis), Times.Once);
        _aiServiceMock.Verify(x => x.GenerateImagePromptAsync(It.IsAny<StoryPhase>()), Times.Exactly(4));
    }

    [Fact]
    public async Task CreateStoryFromPdfAsync_StoresStoryInCache()
    {
        // Arrange
        var fileName = "test.pdf";
        var pdfStream = CreateSimplePdfStream("Test content");
        
        SetupMocksForSuccessfulStoryGeneration();

        // Act
        var result = await _service.CreateStoryFromPdfAsync(pdfStream, fileName);

        // Assert
        var cachedStory = await _service.GetStoryByIdAsync(result.Id);
        cachedStory.Should().NotBeNull();
        cachedStory!.Id.Should().Be(result.Id);
        cachedStory.Title.Should().Be(result.Title);
    }

    [Fact]
    public async Task GetStoryByIdAsync_WithExistingId_ReturnsStory()
    {
        // Arrange
        var story = CreateSampleGeneratedStory();
        _cache.Set(story.Id, story);

        // Act
        var result = await _service.GetStoryByIdAsync(story.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(story.Id);
        result.Title.Should().Be(story.Title);
        result.Phases.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetStoryByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GetStoryByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportStoryAsJsonAsync_WithExistingStory_ReturnsValidJson()
    {
        // Arrange
        var story = CreateSampleGeneratedStory();
        _cache.Set(story.Id, story);

        // Act
        var result = await _service.ExportStoryAsJsonAsync(story.Id);

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var deserializedStory = JsonSerializer.Deserialize<GeneratedStory>(
            result, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        
        deserializedStory.Should().NotBeNull();
        deserializedStory!.Id.Should().Be(story.Id);
        deserializedStory.Title.Should().Be(story.Title);
        deserializedStory.Phases.Should().HaveCount(4);
        deserializedStory.SourceFileName.Should().Be(story.SourceFileName);
    }

    [Fact]
    public async Task ExportStoryAsJsonAsync_WithNonExistingStory_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid().ToString();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExportStoryAsJsonAsync(nonExistingId));
        
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExportStoryAsJsonAsync_IncludesAllPhaseData()
    {
        // Arrange
        var story = CreateSampleGeneratedStory();
        _cache.Set(story.Id, story);

        // Act
        var result = await _service.ExportStoryAsJsonAsync(story.Id);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;
        
        root.GetProperty("id").GetString().Should().Be(story.Id);
        root.GetProperty("title").GetString().Should().Be(story.Title);
        root.GetProperty("sourceFileName").GetString().Should().Be(story.SourceFileName);
        
        var phases = root.GetProperty("phases");
        phases.GetArrayLength().Should().Be(4);
        
        var firstPhase = phases[0];
        firstPhase.GetProperty("name").GetString().Should().Be("Introduction");
        firstPhase.GetProperty("summary").GetString().Should().NotBeNullOrEmpty();
        firstPhase.GetProperty("mood").GetString().Should().NotBeNullOrEmpty();
        firstPhase.GetProperty("imagePrompt").GetString().Should().NotBeNullOrEmpty();
        firstPhase.GetProperty("order").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CreateStoryFromPdfAsync_WhenPdfProcessingFails_ThrowsException()
    {
        // Arrange
        var fileName = "test.pdf";
        var pdfStream = CreateSimplePdfStream("Test content");
        
        _pdfProcessorMock
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new InvalidOperationException("PDF processing failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateStoryFromPdfAsync(pdfStream, fileName));
    }

    [Fact]
    public async Task CreateStoryFromPdfAsync_WhenAiServiceFails_ThrowsException()
    {
        // Arrange
        var fileName = "test.pdf";
        var pdfStream = CreateSimplePdfStream("Test content");
        
        _pdfProcessorMock
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>()))
            .ReturnsAsync("Test content");

        _aiServiceMock
            .Setup(x => x.AnalyzeContentAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateStoryFromPdfAsync(pdfStream, fileName));
    }

    [Fact]
    public async Task ExportStoryAsPdfAsync_WithExistingStory_ReturnsPdfBytes()
    {
        // Arrange
        var story = CreateSampleGeneratedStory();
        story.PdfFilePath = $"generated-stories/{story.Id}.pdf";
        _cache.Set(story.Id, story);

        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _storageServiceMock
            .Setup(x => x.GetStoryPdfAsync(story.PdfFilePath))
            .ReturnsAsync(expectedPdfBytes);

        // Act
        var result = await _service.ExportStoryAsPdfAsync(story.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedPdfBytes);
        _storageServiceMock.Verify(x => x.GetStoryPdfAsync(story.PdfFilePath), Times.Once);
    }

    [Fact]
    public async Task ExportStoryAsPdfAsync_WithNonExistingStory_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid().ToString();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExportStoryAsPdfAsync(nonExistingId));
        
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExportStoryAsPdfAsync_WithMissingPdfFile_GeneratesNewPdf()
    {
        // Arrange
        var story = CreateSampleGeneratedStory();
        story.PdfFilePath = null; // No PDF generated yet
        _cache.Set(story.Id, story);

        var generatedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 };
        _pdfGeneratorMock
            .Setup(x => x.GenerateStoryPdfAsync(It.IsAny<GeneratedStory>()))
            .ReturnsAsync(generatedPdfBytes);

        // Act
        var result = await _service.ExportStoryAsPdfAsync(story.Id);

        // Assert
        result.Should().BeEquivalentTo(generatedPdfBytes);
        _pdfGeneratorMock.Verify(x => x.GenerateStoryPdfAsync(It.IsAny<GeneratedStory>()), Times.Once);
        _storageServiceMock.Verify(x => x.SaveStoryPdfAsync(story.Id, generatedPdfBytes), Times.Once);
    }

    [Fact]
    public async Task CreateStoryFromPdfAsync_GeneratesAndSavesPdf()
    {
        // Arrange
        var fileName = "test.pdf";
        var pdfStream = CreateSimplePdfStream("Test content");
        
        SetupMocksForSuccessfulStoryGeneration();

        // Act
        var result = await _service.CreateStoryFromPdfAsync(pdfStream, fileName);

        // Assert
        result.PdfFilePath.Should().NotBeNullOrEmpty();
        result.PdfFilePath.Should().Contain(result.Id);
        
        _pdfGeneratorMock.Verify(x => x.GenerateStoryPdfAsync(It.IsAny<GeneratedStory>()), Times.Once);
        _storageServiceMock.Verify(x => x.SaveStoryPdfAsync(result.Id, It.IsAny<byte[]>()), Times.Once);
    }

    // Helper methods
    private void SetupMocksForSuccessfulStoryGeneration()
    {
        _pdfProcessorMock
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>()))
            .ReturnsAsync("Test content");

        _aiServiceMock
            .Setup(x => x.AnalyzeContentAsync(It.IsAny<string>()))
            .ReturnsAsync(new ContentAnalysisResult
            {
                KeyFacts = new List<string> { "Fact" },
                Entities = new List<string> { "Entity" },
                Concepts = new List<string> { "Concept" },
                OverallContext = "Context",
                SourceType = "article"
            });

        _aiServiceMock
            .Setup(x => x.GenerateStoryAsync(It.IsAny<ContentAnalysisResult>()))
            .ReturnsAsync(new StoryGenerationResult
            {
                Title = "Test Story",
                Phases = new List<StoryPhaseData>
                {
                    new() { Name = "Introduction", Summary = "Intro", Mood = "mysterious" },
                    new() { Name = "Conflict", Summary = "Conflict", Mood = "tense" },
                    new() { Name = "Climax", Summary = "Climax", Mood = "dramatic" },
                    new() { Name = "Resolution", Summary = "Resolution", Mood = "triumphant" }
                }
            });

        _aiServiceMock
            .Setup(x => x.GenerateImagePromptAsync(It.IsAny<StoryPhase>()))
            .ReturnsAsync((StoryPhase phase) => $"Image prompt for {phase.Name}");
    }

    private GeneratedStory CreateSampleGeneratedStory()
    {
        return new GeneratedStory
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Sample Story",
            SourceFileName = "sample.pdf",
            CreatedAt = DateTime.UtcNow,
            Phases = new List<StoryPhase>
            {
                new()
                {
                    Name = "Introduction",
                    Summary = "The story begins...",
                    Mood = "mysterious",
                    ImagePrompt = "A mysterious scene...",
                    Order = 0
                },
                new()
                {
                    Name = "Conflict",
                    Summary = "Challenges arise...",
                    Mood = "tense",
                    ImagePrompt = "A tense moment...",
                    Order = 1
                },
                new()
                {
                    Name = "Climax",
                    Summary = "The peak of action...",
                    Mood = "dramatic",
                    ImagePrompt = "A dramatic scene...",
                    Order = 2
                },
                new()
                {
                    Name = "Resolution",
                    Summary = "Everything concludes...",
                    Mood = "triumphant",
                    ImagePrompt = "A triumphant ending...",
                    Order = 3
                }
            }
        };
    }

    private MemoryStream CreateSimplePdfStream(string text)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText(text, 12, new PdfPoint(50, 700), font);
        
        var pdfBytes = builder.Build();
        return new MemoryStream(pdfBytes);
    }
}
