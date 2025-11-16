using Dragonscale_Storyteller.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Dragonscale_Storyteller.Tests.Services;

public class PdfProcessorServiceTests
{
    private readonly Mock<ILogger<PdfProcessorService>> _loggerMock;
    private readonly PdfProcessorService _service;

    public PdfProcessorServiceTests()
    {
        _loggerMock = new Mock<ILogger<PdfProcessorService>>();
        _service = new PdfProcessorService(_loggerMock.Object);
    }

    [Fact]
    public void ValidatePdfFile_WithNullFile_ReturnsFalse()
    {
        // Act
        var result = _service.ValidatePdfFile(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePdfFile_WithEmptyFile_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 0);

        // Act
        var result = _service.ValidatePdfFile(fileMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePdfFile_WithFileTooLarge_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 11 * 1024 * 1024); // 11MB

        // Act
        var result = _service.ValidatePdfFile(fileMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePdfFile_WithInvalidContentType_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "text/plain", 1024);

        // Act
        var result = _service.ValidatePdfFile(fileMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePdfFile_WithInvalidExtension_ReturnsFalse()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.txt", "application/pdf", 1024);

        // Act
        var result = _service.ValidatePdfFile(fileMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePdfFile_WithValidPdf_ReturnsTrue()
    {
        // Arrange
        var fileMock = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        // Act
        var result = _service.ValidatePdfFile(fileMock.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractTextFromPdfAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.ExtractTextFromPdfAsync(null!));
    }

    [Fact]
    public async Task ExtractTextFromPdfAsync_WithValidPdf_ExtractsText()
    {
        // Arrange
        var expectedText = "This is a test PDF document.";
        var pdfStream = CreateSimplePdfStream(expectedText);

        // Act
        var result = await _service.ExtractTextFromPdfAsync(pdfStream);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("test");
    }

    [Fact]
    public async Task ExtractTextFromPdfAsync_WithEmptyPdf_ThrowsInvalidOperationException()
    {
        // Arrange
        var pdfStream = CreateSimplePdfStream("");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ExtractTextFromPdfAsync(pdfStream));
        
        exception.Message.Should().Contain("no extractable text");
    }

    [Fact]
    public async Task ExtractTextFromPdfAsync_WithCorruptedPdf_ThrowsInvalidOperationException()
    {
        // Arrange
        var corruptedStream = new MemoryStream(Encoding.UTF8.GetBytes("This is not a valid PDF"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.ExtractTextFromPdfAsync(corruptedStream));
        
        exception.Message.Should().Contain("corrupted or unreadable");
    }

    [Fact]
    public async Task ExtractTextFromPdfAsync_WithMultiPagePdf_ExtractsAllPages()
    {
        // Arrange
        var page1Text = "Page 1 content";
        var page2Text = "Page 2 content";
        var pdfStream = CreateMultiPagePdfStream(new[] { page1Text, page2Text });

        // Act
        var result = await _service.ExtractTextFromPdfAsync(pdfStream);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("Page 1");
        result.Should().Contain("Page 2");
    }

    // Helper methods
    private Mock<IFormFile> CreateMockFormFile(string fileName, string contentType, long length)
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.Length).Returns(length);
        return fileMock;
    }

    private MemoryStream CreateSimplePdfStream(string text)
    {
        var builder = new PdfDocumentBuilder();
        
        if (!string.IsNullOrWhiteSpace(text))
        {
            var page = builder.AddPage(PageSize.A4);
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            page.AddText(text, 12, new PdfPoint(50, 700), font);
        }
        else
        {
            // Create a page with no text
            builder.AddPage(PageSize.A4);
        }

        var pdfBytes = builder.Build();
        return new MemoryStream(pdfBytes);
    }

    private MemoryStream CreateMultiPagePdfStream(string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var text in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            page.AddText(text, 12, new PdfPoint(50, 700), font);
        }

        var pdfBytes = builder.Build();
        return new MemoryStream(pdfBytes);
    }
}
