using Microsoft.AspNetCore.Mvc;
using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Services;
using Dragonscale_Storyteller.Exceptions;

namespace Dragonscale_Storyteller.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoryGeneratorController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly IPdfProcessorService _pdfProcessor;
    private readonly ILogger<StoryGeneratorController> _logger;

    public StoryGeneratorController(
        IStoryService storyService,
        IPdfProcessorService pdfProcessor,
        ILogger<StoryGeneratorController> logger)
    {
        _storyService = storyService;
        _pdfProcessor = pdfProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Upload a PDF file and generate a story from its content
    /// </summary>
    /// <param name="file">The PDF file to process</param>
    /// <param name="language">Story language (de or en)</param>
    /// <param name="mood">Story mood (neutral, happy, sad, horror, dramatic)</param>
    /// <param name="keywords">Optional keywords (comma-separated)</param>
    /// <returns>Story response with generated story ID and data</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(StoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadPdf(
        IFormFile file,
        [FromForm] string language = "de",
        [FromForm] string mood = "neutral",
        [FromForm] string? keywords = null)
    {
        try
        {
            _logger.LogInformation("Received PDF upload request: {FileName}", file?.FileName ?? "null");

            // Validate file
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload failed: No file provided");
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "NO_FILE",
                    Message = "No file was provided in the request",
                    UserFriendlyMessage = "Please select a PDF file to upload",
                    Timestamp = DateTime.UtcNow
                });
            }

            // Validate PDF file format and size
            if (!_pdfProcessor.ValidatePdfFile(file))
            {
                _logger.LogWarning("Upload failed: Invalid PDF file - {FileName}", file.FileName);
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "INVALID_FILE",
                    Message = $"File validation failed for {file.FileName}",
                    UserFriendlyMessage = "The file must be a valid PDF and not exceed 10MB in size",
                    Timestamp = DateTime.UtcNow
                });
            }

            // Parse configuration
            var config = new StoryConfiguration
            {
                Language = language,
                Mood = mood,
                Keywords = string.IsNullOrWhiteSpace(keywords) 
                    ? new List<string>() 
                    : keywords.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList()
            };

            _logger.LogInformation("Story configuration: Language={Language}, Mood={Mood}, Keywords={Keywords}", 
                config.Language, config.Mood, string.Join(", ", config.Keywords));

            // Process PDF and generate story
            using var stream = file.OpenReadStream();
            var story = await _storyService.CreateStoryFromPdfAsync(stream, file.FileName, config);

            _logger.LogInformation("Story generated successfully: {StoryId} from {FileName}", 
                story.Id, file.FileName);

            return Ok(new StoryResponse
            {
                Success = true,
                StoryId = story.Id,
                Story = story,
                ErrorMessage = null
            });
        }
        catch (PdfProcessingException ex)
        {
            _logger.LogError(ex, "PDF processing error for file: {FileName}, ErrorType: {ErrorType}", 
                file?.FileName, ex.ErrorType);
            
            var userMessage = ex.ErrorType switch
            {
                PdfProcessingErrorType.InvalidFormat => "The file format is not supported. Please upload a valid PDF file.",
                PdfProcessingErrorType.FileSizeExceeded => "The file size exceeds the maximum allowed size of 10MB.",
                PdfProcessingErrorType.CorruptedFile => "The PDF file appears to be corrupted or unreadable.",
                PdfProcessingErrorType.NoTextContent => "The PDF file contains no extractable text content.",
                PdfProcessingErrorType.ExtractionFailed => "Failed to extract text from the PDF file.",
                _ => "Unable to process the PDF file."
            };
            
            return BadRequest(new ErrorResponse
            {
                ErrorCode = $"PDF_{ex.ErrorType.ToString().ToUpper()}",
                Message = ex.Message,
                UserFriendlyMessage = userMessage,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI service error for file: {FileName}, ErrorType: {ErrorType}", 
                file?.FileName, ex.ErrorType);
            
            var userMessage = ex.ErrorType switch
            {
                AiServiceErrorType.AuthenticationFailed => "AI service authentication failed. Please contact support.",
                AiServiceErrorType.RateLimitExceeded => "Too many requests. Please try again in a few moments.",
                AiServiceErrorType.ServiceUnavailable => "AI service is temporarily unavailable. Please try again later.",
                AiServiceErrorType.InvalidResponse => "Received an invalid response from AI service. Please try again.",
                _ => "An error occurred while generating the story. Please try again."
            };
            
            var statusCode = ex.ErrorType == AiServiceErrorType.RateLimitExceeded 
                ? StatusCodes.Status429TooManyRequests 
                : StatusCodes.Status503ServiceUnavailable;
            
            return StatusCode(statusCode, new ErrorResponse
            {
                ErrorCode = $"AI_{ex.ErrorType.ToString().ToUpper()}",
                Message = ex.Message,
                UserFriendlyMessage = userMessage,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error for file: {FileName}, ErrorType: {ErrorType}", 
                file?.FileName, ex.ErrorType);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = $"STORAGE_{ex.ErrorType.ToString().ToUpper()}",
                Message = ex.Message,
                UserFriendlyMessage = "Failed to save the generated story. Please try again.",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during story generation for file: {FileName}: {ErrorMessage}", 
                file?.FileName, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An unexpected error occurred during story generation",
                UserFriendlyMessage = "We encountered an error while processing your file. Please try again later.",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Retrieve a generated story by its ID
    /// </summary>
    /// <param name="id">The unique story identifier</param>
    /// <returns>Story response with story data</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(StoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStory(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving story: {StoryId}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Get story failed: Invalid story ID");
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "INVALID_ID",
                    Message = "Story ID cannot be empty",
                    UserFriendlyMessage = "Please provide a valid story ID",
                    Timestamp = DateTime.UtcNow
                });
            }

            var story = await _storyService.GetStoryByIdAsync(id);

            if (story == null)
            {
                _logger.LogWarning("Story not found: {StoryId}", id);
                return NotFound(new ErrorResponse
                {
                    ErrorCode = "STORY_NOT_FOUND",
                    Message = $"Story with ID '{id}' was not found",
                    UserFriendlyMessage = "The requested story could not be found. It may have expired or never existed.",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Story retrieved successfully: {StoryId}", id);

            return Ok(new StoryResponse
            {
                Success = true,
                StoryId = story.Id,
                Story = story,
                ErrorMessage = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving story: {StoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while retrieving the story",
                UserFriendlyMessage = "We encountered an error while retrieving your story. Please try again later.",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Export a story as JSON
    /// </summary>
    /// <param name="id">The unique story identifier</param>
    /// <returns>JSON file download</returns>
    [HttpGet("{id}/export/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportStoryAsJson(string id)
    {
        try
        {
            _logger.LogInformation("Exporting story as JSON: {StoryId}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Export JSON failed: Invalid story ID");
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "INVALID_ID",
                    Message = "Story ID cannot be empty",
                    UserFriendlyMessage = "Please provide a valid story ID",
                    Timestamp = DateTime.UtcNow
                });
            }

            var jsonContent = await _storyService.ExportStoryAsJsonAsync(id);

            _logger.LogInformation("Story exported as JSON successfully: {StoryId}", id);

            var fileName = $"story-{id}.json";
            var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);

            return File(bytes, "application/json", fileName);
        }
        catch (StoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Story not found for JSON export: {StoryId}", id);
            return NotFound(new ErrorResponse
            {
                ErrorCode = "STORY_NOT_FOUND",
                Message = ex.Message,
                UserFriendlyMessage = "The requested story could not be found. It may have expired or never existed.",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting story as JSON: {StoryId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while exporting the story",
                UserFriendlyMessage = "We encountered an error while exporting your story. Please try again later.",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Export a story as PDF
    /// </summary>
    /// <param name="id">The unique story identifier</param>
    /// <returns>PDF file download</returns>
    [HttpGet("{id}/export/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportStoryAsPdf(string id)
    {
        try
        {
            _logger.LogInformation("Exporting story as PDF: {StoryId}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Export PDF failed: Invalid story ID");
                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "INVALID_ID",
                    Message = "Story ID cannot be empty",
                    UserFriendlyMessage = "Please provide a valid story ID",
                    Timestamp = DateTime.UtcNow
                });
            }

            var pdfBytes = await _storyService.ExportStoryAsPdfAsync(id);

            _logger.LogInformation("Story exported as PDF successfully: {StoryId}", id);

            var fileName = $"story-{id}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (StoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Story not found for PDF export: {StoryId}", id);
            return NotFound(new ErrorResponse
            {
                ErrorCode = "STORY_NOT_FOUND",
                Message = ex.Message,
                UserFriendlyMessage = "The requested story could not be found. It may have expired or never existed.",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error during PDF export: {StoryId}, ErrorType: {ErrorType}", 
                id, ex.ErrorType);
            
            var statusCode = ex.ErrorType == StorageErrorType.FileNotFound 
                ? StatusCodes.Status404NotFound 
                : StatusCodes.Status500InternalServerError;
            
            return StatusCode(statusCode, new ErrorResponse
            {
                ErrorCode = $"STORAGE_{ex.ErrorType.ToString().ToUpper()}",
                Message = ex.Message,
                UserFriendlyMessage = "Failed to retrieve the PDF file. Please try regenerating the story.",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting story as PDF: {StoryId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An unexpected error occurred while exporting the story",
                UserFriendlyMessage = "We encountered an error while exporting your story. Please try again later.",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
