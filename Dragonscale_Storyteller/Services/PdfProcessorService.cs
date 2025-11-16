using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using Dragonscale_Storyteller.Exceptions;

namespace Dragonscale_Storyteller.Services;

public class PdfProcessorService : IPdfProcessorService
{
    private readonly ILogger<PdfProcessorService> _logger;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedContentTypes = { "application/pdf" };
    private static readonly string[] AllowedExtensions = { ".pdf" };

    public PdfProcessorService(ILogger<PdfProcessorService> logger)
    {
        _logger = logger;
    }

    public bool ValidatePdfFile(IFormFile file)
    {
        if (file == null)
        {
            _logger.LogWarning("File validation failed: file is null");
            return false;
        }

        // Check file size
        if (file.Length == 0)
        {
            _logger.LogWarning("File validation failed: file is empty");
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            _logger.LogWarning("File validation failed: file size {Size} exceeds maximum {MaxSize}", 
                file.Length, MaxFileSizeBytes);
            return false;
        }

        // Check content type
        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("File validation failed: invalid content type {ContentType}", 
                file.ContentType);
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || 
            !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("File validation failed: invalid extension {Extension}", extension);
            return false;
        }

        _logger.LogInformation("File validation successful: {FileName}, Size: {Size} bytes", 
            file.FileName, file.Length);
        return true;
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        if (pdfStream == null)
        {
            _logger.LogError("PDF stream is null");
            throw new ArgumentNullException(nameof(pdfStream));
        }

        try
        {
            _logger.LogInformation("Starting PDF text extraction");

            var extractedText = new StringBuilder();

            // PdfPig requires a seekable stream
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using (var document = PdfDocument.Open(memoryStream))
            {
                _logger.LogInformation("PDF opened successfully. Page count: {PageCount}", 
                    document.NumberOfPages);

                if (document.NumberOfPages == 0)
                {
                    _logger.LogWarning("PDF document contains no pages");
                    throw new PdfProcessingException(
                        "PDF document contains no pages",
                        PdfProcessingErrorType.NoTextContent);
                }

                foreach (Page page in document.GetPages())
                {
                    var pageText = page.Text;
                    
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        extractedText.AppendLine(pageText);
                        extractedText.AppendLine(); // Add spacing between pages
                    }
                }
            }

            var result = extractedText.ToString().Trim();

            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("PDF document contains no extractable text");
                throw new PdfProcessingException(
                    "PDF document contains no extractable text",
                    PdfProcessingErrorType.NoTextContent);
            }

            _logger.LogInformation("PDF text extraction completed. Extracted {Length} characters", 
                result.Length);

            return result;
        }
        catch (PdfProcessingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {ErrorMessage}", ex.Message);
            throw new PdfProcessingException(
                "Failed to extract text from PDF. The file may be corrupted or unreadable.",
                ex,
                PdfProcessingErrorType.CorruptedFile);
        }
    }
}
