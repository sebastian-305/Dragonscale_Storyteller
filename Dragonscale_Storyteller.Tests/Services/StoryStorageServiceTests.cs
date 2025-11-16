using Dragonscale_Storyteller.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dragonscale_Storyteller.Tests.Services;

public class StoryStorageServiceTests : IDisposable
{
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<StoryStorageService>> _loggerMock;
    private readonly StoryStorageService _service;
    private readonly string _testStoragePath;

    public StoryStorageServiceTests()
    {
        _environmentMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<StoryStorageService>>();

        // Create a temporary directory for testing
        _testStoragePath = Path.Combine(Path.GetTempPath(), "DragonscaleTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testStoragePath);

        _environmentMock.Setup(x => x.WebRootPath).Returns(_testStoragePath);

        _service = new StoryStorageService(_environmentMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SaveStoryPdfAsync_WithValidData_SavesFileSuccessfully()
    {
        // Arrange
        var storyId = Guid.NewGuid().ToString("N");
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header bytes

        // Act
        var result = await _service.SaveStoryPdfAsync(storyId, pdfContent);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(storyId);
        result.Should().EndWith(".pdf");

        var fullPath = Path.Combine(_testStoragePath, result);
        File.Exists(fullPath).Should().BeTrue();

        var savedContent = await File.ReadAllBytesAsync(fullPath);
        savedContent.Should().BeEquivalentTo(pdfContent);
    }

    [Fact]
    public async Task SaveStoryPdfAsync_ReturnsRelativePath()
    {
        // Arrange
        var storyId = Guid.NewGuid().ToString("N");
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        // Act
        var result = await _service.SaveStoryPdfAsync(storyId, pdfContent);

        // Assert
        result.Should().StartWith("generated-stories");
        result.Should().Contain(storyId);
    }

    [Fact]
    public async Task GetStoryPdfAsync_WithExistingFile_ReturnsFileContent()
    {
        // Arrange
        var storyId = Guid.NewGuid().ToString("N");
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        
        var relativePath = await _service.SaveStoryPdfAsync(storyId, pdfContent);

        // Act
        var result = await _service.GetStoryPdfAsync(relativePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(pdfContent);
    }

    [Fact]
    public async Task GetStoryPdfAsync_WithNonExistingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistingPath = "generated-stories/nonexistent.pdf";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.GetStoryPdfAsync(nonExistingPath));
    }

    [Fact]
    public async Task SaveStoryPdfAsync_CreatesStorageDirectoryIfNotExists()
    {
        // Arrange
        var newTestPath = Path.Combine(Path.GetTempPath(), "DragonscaleTests", Guid.NewGuid().ToString());
        var newEnvironmentMock = new Mock<IWebHostEnvironment>();
        newEnvironmentMock.Setup(x => x.WebRootPath).Returns(newTestPath);

        var newService = new StoryStorageService(newEnvironmentMock.Object, _loggerMock.Object);

        var storyId = Guid.NewGuid().ToString("N");
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        // Act
        var result = await newService.SaveStoryPdfAsync(storyId, pdfContent);

        // Assert
        var storageDir = Path.Combine(newTestPath, "generated-stories");
        Directory.Exists(storageDir).Should().BeTrue();

        // Cleanup
        if (Directory.Exists(newTestPath))
        {
            Directory.Delete(newTestPath, true);
        }
    }

    [Fact]
    public async Task SaveStoryPdfAsync_WithLargeFile_SavesSuccessfully()
    {
        // Arrange
        var storyId = Guid.NewGuid().ToString("N");
        var pdfContent = new byte[1024 * 1024]; // 1 MB
        new Random().NextBytes(pdfContent);

        // Act
        var result = await _service.SaveStoryPdfAsync(storyId, pdfContent);

        // Assert
        var fullPath = Path.Combine(_testStoragePath, result);
        var savedContent = await File.ReadAllBytesAsync(fullPath);
        savedContent.Length.Should().Be(pdfContent.Length);
    }

    public void Dispose()
    {
        // Cleanup test directory
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
    }
}
