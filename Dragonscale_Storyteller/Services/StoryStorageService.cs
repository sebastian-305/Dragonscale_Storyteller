using Dragonscale_Storyteller.Exceptions;

namespace Dragonscale_Storyteller.Services;

public class StoryStorageService : IStoryStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StoryStorageService> _logger;
    private const string StorageFolder = "generated-stories";

    public StoryStorageService(
        IWebHostEnvironment environment,
        ILogger<StoryStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        
        // Ensure storage directory exists
        EnsureStorageDirectoryExists();
    }

    public async Task<string> SaveStoryPdfAsync(string storyId, byte[] pdfContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                _logger.LogError("Story ID is null or empty");
                throw new ArgumentNullException(nameof(storyId));
            }

            if (pdfContent == null || pdfContent.Length == 0)
            {
                _logger.LogError("PDF content is null or empty for story {StoryId}", storyId);
                throw new ArgumentException("PDF content cannot be null or empty", nameof(pdfContent));
            }

            var fileName = $"{storyId}.pdf";
            var storagePath = GetStoragePath();
            var filePath = Path.Combine(storagePath, fileName);

            _logger.LogInformation("Saving PDF for story {StoryId} to {FilePath}", storyId, filePath);

            await File.WriteAllBytesAsync(filePath, pdfContent);

            _logger.LogInformation("PDF saved successfully for story {StoryId}, Size: {Size} bytes", 
                storyId, pdfContent.Length);

            // Return relative path for storage in database/cache
            return Path.Combine(StorageFolder, fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when saving PDF for story {StoryId}", storyId);
            throw new StorageException(
                "Access denied when saving PDF file",
                ex,
                StorageErrorType.SaveFailed,
                storyId);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error saving PDF for story {StoryId}: {ErrorMessage}", storyId, ex.Message);
            throw new StorageException(
                $"Failed to save PDF file: {ex.Message}",
                ex,
                StorageErrorType.SaveFailed,
                storyId);
        }
        catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error saving PDF for story {StoryId}: {ErrorMessage}", storyId, ex.Message);
            throw new StorageException(
                $"Unexpected error saving PDF file: {ex.Message}",
                ex,
                StorageErrorType.SaveFailed,
                storyId);
        }
    }

    public async Task<byte[]> GetStoryPdfAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogError("File path is null or empty");
                throw new ArgumentNullException(nameof(filePath));
            }

            var fullPath = Path.Combine(_environment.WebRootPath, filePath);

            _logger.LogInformation("Retrieving PDF from {FilePath}", fullPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("PDF file not found at {FilePath}", fullPath);
                throw new StorageException(
                    $"PDF file not found: {filePath}",
                    StorageErrorType.FileNotFound,
                    filePath);
            }

            var pdfContent = await File.ReadAllBytesAsync(fullPath);

            _logger.LogInformation("PDF retrieved successfully from {FilePath}, Size: {Size} bytes", 
                fullPath, pdfContent.Length);

            return pdfContent;
        }
        catch (StorageException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when retrieving PDF from {FilePath}", filePath);
            throw new StorageException(
                "Access denied when reading PDF file",
                ex,
                StorageErrorType.ReadFailed,
                filePath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error retrieving PDF from {FilePath}: {ErrorMessage}", filePath, ex.Message);
            throw new StorageException(
                $"Failed to read PDF file: {ex.Message}",
                ex,
                StorageErrorType.ReadFailed,
                filePath);
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Unexpected error retrieving PDF from {FilePath}: {ErrorMessage}", filePath, ex.Message);
            throw new StorageException(
                $"Unexpected error reading PDF file: {ex.Message}",
                ex,
                StorageErrorType.ReadFailed,
                filePath);
        }
    }

    private string GetStoragePath()
    {
        return Path.Combine(_environment.WebRootPath, StorageFolder);
    }

    private void EnsureStorageDirectoryExists()
    {
        try
        {
            var storagePath = GetStoragePath();
            
            if (!Directory.Exists(storagePath))
            {
                _logger.LogInformation("Creating storage directory at {StoragePath}", storagePath);
                Directory.CreateDirectory(storagePath);
                _logger.LogInformation("Storage directory created successfully at {StoragePath}", storagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create storage directory: {ErrorMessage}", ex.Message);
            throw new StorageException(
                $"Failed to create storage directory: {ex.Message}",
                ex,
                StorageErrorType.DirectoryCreationFailed);
        }
    }
}
