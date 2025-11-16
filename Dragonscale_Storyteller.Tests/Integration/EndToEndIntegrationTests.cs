using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dragonscale_Storyteller.Services;
using Dragonscale_Storyteller.Configuration;
using Dragonscale_Storyteller.Models;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

namespace Dragonscale_Storyteller.Tests.Integration;

/// <summary>
/// End-to-end integration tests for the complete story generation pipeline
/// Tests: Task 10.1 - Test complete end-to-end workflow
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IStoryService _storyService;
    private readonly IPdfProcessorService _pdfProcessor;
    private readonly string _testStoragePath;

    public EndToEndIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"test-stories-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testStoragePath);

        // Build configuration - supports appsettings.json, User Secrets, and Environment Variables
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<EndToEndIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup DI container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configure services
        services.Configure<NebiusAiConfiguration>(configuration.GetSection("NebiusAi"));
        services.Configure<StorageConfiguration>(options =>
        {
            options.GeneratedStoriesPath = _testStoragePath;
        });

        services.AddMemoryCache();
        services.AddScoped<IPdfProcessorService, PdfProcessorService>();
        services.AddScoped<IAiService, NebiusAiService>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
        services.AddScoped<IStoryStorageService, StoryStorageService>();
        services.AddScoped<IStoryService, StoryService>();

        _serviceProvider = services.BuildServiceProvider();
        _storyService = _serviceProvider.GetRequiredService<IStoryService>();
        _pdfProcessor = _serviceProvider.GetRequiredService<IPdfProcessorService>();
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task EndToEnd_TechnicalManual_GeneratesCreativeStory()
    {
        // Arrange
        _output.WriteLine("Test: Technical Manual -> Creative Story");
        var pdfContent = CreateTestPdf_TechnicalManual();
        using var stream = new MemoryStream(pdfContent);

        // Act
        _output.WriteLine("Step 1: Creating story from PDF...");
        var story = await _storyService.CreateStoryFromPdfAsync(stream, "technical-manual.pdf");

        // Assert
        _output.WriteLine($"Story generated: {story.Title}");
        Assert.NotNull(story);
        Assert.NotNull(story.Id);
        Assert.NotEmpty(story.Title);
        Assert.Equal(4, story.Phases.Count);
        
        // Verify phases
        foreach (var phase in story.Phases)
        {
            _output.WriteLine($"Phase {phase.Order}: {phase.Name} - Mood: {phase.Mood}");
            Assert.NotEmpty(phase.Name);
            Assert.NotEmpty(phase.Summary);
            Assert.NotEmpty(phase.Mood);
            Assert.NotEmpty(phase.ImagePrompt);
            _output.WriteLine($"  Summary: {phase.Summary[..Math.Min(100, phase.Summary.Length)]}...");
            _output.WriteLine($"  Image Prompt: {phase.ImagePrompt[..Math.Min(100, phase.ImagePrompt.Length)]}...");
        }

        // Test retrieval
        _output.WriteLine("\nStep 2: Retrieving story by ID...");
        var retrievedStory = await _storyService.GetStoryByIdAsync(story.Id);
        Assert.NotNull(retrievedStory);
        Assert.Equal(story.Id, retrievedStory.Id);

        // Test JSON export
        _output.WriteLine("\nStep 3: Exporting as JSON...");
        var jsonExport = await _storyService.ExportStoryAsJsonAsync(story.Id);
        Assert.NotEmpty(jsonExport);
        Assert.Contains(story.Title, jsonExport);
        _output.WriteLine($"JSON export size: {jsonExport.Length} characters");

        // Test PDF export
        _output.WriteLine("\nStep 4: Exporting as PDF...");
        var pdfExport = await _storyService.ExportStoryAsPdfAsync(story.Id);
        Assert.NotNull(pdfExport);
        Assert.True(pdfExport.Length > 0);
        _output.WriteLine($"PDF export size: {pdfExport.Length} bytes");

        // Verify PDF was saved
        Assert.NotNull(story.PdfFilePath);
        Assert.True(File.Exists(story.PdfFilePath));
        
        _output.WriteLine("\n✓ End-to-end test completed successfully!");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task EndToEnd_NewsArticle_GeneratesCreativeStory()
    {
        // Arrange
        _output.WriteLine("Test: News Article -> Creative Story");
        var pdfContent = CreateTestPdf_NewsArticle();
        using var stream = new MemoryStream(pdfContent);

        // Act
        _output.WriteLine("Creating story from news article PDF...");
        var story = await _storyService.CreateStoryFromPdfAsync(stream, "news-article.pdf");

        // Assert
        _output.WriteLine($"Story generated: {story.Title}");
        Assert.NotNull(story);
        Assert.Equal(4, story.Phases.Count);
        
        // Verify creativity - story should transform news into narrative
        Assert.NotEmpty(story.Title);
        _output.WriteLine($"Creative title: {story.Title}");
        
        foreach (var phase in story.Phases)
        {
            _output.WriteLine($"Phase: {phase.Name} ({phase.Mood})");
            Assert.NotEmpty(phase.ImagePrompt);
        }
        
        _output.WriteLine("\n✓ News article transformation completed!");
    }

    [Fact(Skip = "Requires valid Nebius AI API key")]
    public async Task EndToEnd_ShoppingList_GeneratesCreativeStory()
    {
        // Arrange
        _output.WriteLine("Test: Shopping List -> Creative Story");
        var pdfContent = CreateTestPdf_ShoppingList();
        using var stream = new MemoryStream(pdfContent);

        // Act
        _output.WriteLine("Creating story from shopping list PDF...");
        var story = await _storyService.CreateStoryFromPdfAsync(stream, "shopping-list.pdf");

        // Assert
        _output.WriteLine($"Story generated: {story.Title}");
        Assert.NotNull(story);
        Assert.Equal(4, story.Phases.Count);
        
        // Verify that mundane list was transformed creatively
        _output.WriteLine($"Creative transformation: {story.Title}");
        Assert.NotEmpty(story.Title);
        
        _output.WriteLine("\n✓ Shopping list transformation completed!");
    }

    [Fact]
    public async Task EndToEnd_ErrorScenario_CorruptedPdf_HandlesGracefully()
    {
        // Arrange
        _output.WriteLine("Test: Error Handling - Corrupted PDF");
        var corruptedPdf = Encoding.UTF8.GetBytes("This is not a valid PDF file");
        using var stream = new MemoryStream(corruptedPdf);

        // Act & Assert
        _output.WriteLine("Attempting to process corrupted PDF...");
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _storyService.CreateStoryFromPdfAsync(stream, "corrupted.pdf");
        });
        
        _output.WriteLine("✓ Corrupted PDF handled correctly with exception");
    }

    [Fact]
    public async Task EndToEnd_ErrorScenario_EmptyPdf_HandlesGracefully()
    {
        // Arrange
        _output.WriteLine("Test: Error Handling - Empty PDF");
        var emptyPdf = CreateTestPdf_Empty();
        using var stream = new MemoryStream(emptyPdf);

        // Act & Assert
        _output.WriteLine("Attempting to process empty PDF...");
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _storyService.CreateStoryFromPdfAsync(stream, "empty.pdf");
        });
        
        _output.WriteLine("✓ Empty PDF handled correctly with exception");
    }

    [Fact]
    public async Task EndToEnd_StoryRetrieval_NonExistentId_ReturnsNull()
    {
        // Arrange
        _output.WriteLine("Test: Story Retrieval - Non-existent ID");
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        _output.WriteLine($"Attempting to retrieve story with ID: {nonExistentId}");
        var story = await _storyService.GetStoryByIdAsync(nonExistentId);

        // Assert
        Assert.Null(story);
        _output.WriteLine("✓ Non-existent story correctly returned null");
    }

    // Helper methods to create test PDFs
    private byte[] CreateTestPdf_TechnicalManual()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        
        page.AddText("TECHNICAL MANUAL", 12, new PdfPoint(50, 800), font);
        page.AddText("Model: XR-2000 Coffee Maker", 10, new PdfPoint(50, 780), font);
        page.AddText("SAFETY INSTRUCTIONS:", 10, new PdfPoint(50, 740), font);
        page.AddText("1. Always unplug before cleaning", 10, new PdfPoint(50, 720), font);
        page.AddText("2. Do not immerse in water", 10, new PdfPoint(50, 700), font);
        page.AddText("3. Keep away from children", 10, new PdfPoint(50, 680), font);
        page.AddText("OPERATION:", 10, new PdfPoint(50, 640), font);
        page.AddText("1. Fill water reservoir", 10, new PdfPoint(50, 620), font);
        page.AddText("2. Add coffee grounds to filter", 10, new PdfPoint(50, 600), font);
        page.AddText("3. Press power button", 10, new PdfPoint(50, 580), font);
        page.AddText("4. Wait for brewing cycle to complete", 10, new PdfPoint(50, 560), font);
        page.AddText("TROUBLESHOOTING:", 10, new PdfPoint(50, 520), font);
        page.AddText("Problem: Machine won't start", 10, new PdfPoint(50, 500), font);
        page.AddText("Solution: Check power connection", 10, new PdfPoint(50, 480), font);
        
        return builder.Build();
    }

    private byte[] CreateTestPdf_NewsArticle()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        
        page.AddText("LOCAL NEWS", 12, new PdfPoint(50, 800), font);
        page.AddText("City Park Renovation Complete", 11, new PdfPoint(50, 760), font);
        page.AddText("By Jane Reporter", 9, new PdfPoint(50, 740), font);
        page.AddText("After six months of construction, the Central Park renovation", 10, new PdfPoint(50, 700), font);
        page.AddText("project has been completed. The park now features new", 10, new PdfPoint(50, 680), font);
        page.AddText("playground equipment, walking paths, and a community garden.", 10, new PdfPoint(50, 660), font);
        page.AddText("Mayor Smith stated: 'This park will serve our community", 10, new PdfPoint(50, 620), font);
        page.AddText("for generations to come.' The grand opening ceremony", 10, new PdfPoint(50, 600), font);
        page.AddText("is scheduled for next Saturday at 10 AM.", 10, new PdfPoint(50, 580), font);
        
        return builder.Build();
    }

    private byte[] CreateTestPdf_ShoppingList()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        
        page.AddText("SHOPPING LIST", 12, new PdfPoint(50, 800), font);
        page.AddText("Groceries:", 10, new PdfPoint(50, 760), font);
        page.AddText("- Milk (2 liters)", 10, new PdfPoint(50, 740), font);
        page.AddText("- Bread (whole wheat)", 10, new PdfPoint(50, 720), font);
        page.AddText("- Eggs (dozen)", 10, new PdfPoint(50, 700), font);
        page.AddText("- Apples (6)", 10, new PdfPoint(50, 680), font);
        page.AddText("- Chicken breast (1 kg)", 10, new PdfPoint(50, 660), font);
        page.AddText("- Pasta", 10, new PdfPoint(50, 640), font);
        page.AddText("- Tomato sauce", 10, new PdfPoint(50, 620), font);
        page.AddText("- Cheese", 10, new PdfPoint(50, 600), font);
        page.AddText("Household:", 10, new PdfPoint(50, 560), font);
        page.AddText("- Paper towels", 10, new PdfPoint(50, 540), font);
        page.AddText("- Dish soap", 10, new PdfPoint(50, 520), font);
        page.AddText("- Laundry detergent", 10, new PdfPoint(50, 500), font);
        
        return builder.Build();
    }

    private byte[] CreateTestPdf_Empty()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        return builder.Build();
    }

    public void Dispose()
    {
        // Cleanup test storage
        if (Directory.Exists(_testStoragePath))
        {
            try
            {
                Directory.Delete(_testStoragePath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _serviceProvider?.Dispose();
    }
}
