using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Dragonscale_Storyteller.Services;

public class StoryService : IStoryService
{
    private readonly IPdfProcessorService _pdfProcessor;
    private readonly IAiService _aiService;
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IStoryStorageService _storageService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StoryService> _logger;
    private const int CacheExpirationHours = 24;

    public StoryService(
        IPdfProcessorService pdfProcessor,
        IAiService aiService,
        IPdfGeneratorService pdfGenerator,
        IStoryStorageService storageService,
        IMemoryCache cache,
        ILogger<StoryService> logger)
    {
        _pdfProcessor = pdfProcessor;
        _aiService = aiService;
        _pdfGenerator = pdfGenerator;
        _storageService = storageService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeneratedStory> CreateStoryFromPdfAsync(Stream pdfStream, string fileName, StoryConfiguration? config = null)
    {
        var pipelineStartTime = DateTime.UtcNow;
        
        try
        {
            if (pdfStream == null)
            {
                _logger.LogError("PDF stream is null");
                throw new ArgumentNullException(nameof(pdfStream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("File name is null or empty");
                throw new ArgumentNullException(nameof(fileName));
            }

            // Use default configuration if none provided
            config ??= new StoryConfiguration();
            
            _logger.LogInformation("Starting story generation pipeline for file: {FileName} with config: Language={Language}, Mood={Mood}, Keywords={Keywords}", 
                fileName, config.Language, config.Mood, string.Join(", ", config.Keywords));

            // Step 1: Extract text from PDF
            _logger.LogInformation("Step 1: Extracting text from PDF");
            var extractedText = await _pdfProcessor.ExtractTextFromPdfAsync(pdfStream);

            // Step 2: Analyze content
            _logger.LogInformation("Step 2: Analyzing content");
            var analysis = await _aiService.AnalyzeContentAsync(extractedText);

            // Step 3: Generate story with configuration
            _logger.LogInformation("Step 3: Generating story with configuration");
            var storyResult = await _aiService.GenerateStoryAsync(analysis, config);

            // Step 4: Generate image prompts and images for each phase
            _logger.LogInformation("Step 4: Generating image prompts and images for {PhaseCount} phases", storyResult.Phases.Count);
            var phases = new List<StoryPhase>();
            
            for (int i = 0; i < storyResult.Phases.Count; i++)
            {
                var phaseData = storyResult.Phases[i];
                
                // Create StoryPhase object
                var phase = new StoryPhase
                {
                    Name = phaseData.Name,
                    Summary = phaseData.Summary,
                    Mood = phaseData.Mood,
                    Order = i,
                    ImagePrompt = string.Empty // Will be populated next
                };

                // Generate image prompt for this phase
                _logger.LogInformation("Generating image prompt for phase {Order}: {PhaseName}", i, phase.Name);
                phase.ImagePrompt = await _aiService.GenerateImagePromptAsync(phase);
                
                // Generate actual image from the prompt
                _logger.LogInformation("Generating image for phase {Order}: {PhaseName}", i, phase.Name);
                try
                {
                    var imageBytes = await _aiService.GenerateImageAsync(phase.ImagePrompt);
                    
                    // Convert to base64 for JSON serialization
                    phase.ImageData = Convert.ToBase64String(imageBytes);
                    
                    _logger.LogInformation("Image generated successfully for phase {Order}, size: {Size} bytes", 
                        i, imageBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate image for phase {Order}: {PhaseName}. Continuing without image.", 
                        i, phase.Name);
                    // Continue without image - the story can still be generated
                    phase.ImageData = null;
                }
                
                phases.Add(phase);
            }

            // Step 5: Create GeneratedStory object
            var story = new GeneratedStory
            {
                Id = GenerateUniqueStoryId(),
                Title = storyResult.Title,
                Phases = phases,
                CreatedAt = DateTime.UtcNow,
                SourceFileName = fileName,
                PdfFilePath = null // Will be set after PDF generation
            };

            // Step 6: Generate and save PDF
            _logger.LogInformation("Step 5: Generating PDF for story");
            var pdfBytes = await _pdfGenerator.GenerateStoryPdfAsync(story);
            
            _logger.LogInformation("Step 6: Saving PDF to storage");
            var pdfFilePath = await _storageService.SaveStoryPdfAsync(story.Id, pdfBytes);
            story.PdfFilePath = pdfFilePath;

            // Step 7: Store in cache
            _logger.LogInformation("Step 7: Storing story in cache with ID: {StoryId}", story.Id);
            StoreStoryInCache(story);

            var pipelineDuration = DateTime.UtcNow - pipelineStartTime;
            _logger.LogInformation("Story generation pipeline completed successfully for {FileName}. Story ID: {StoryId}, Total Duration: {Duration}ms", 
                fileName, story.Id, pipelineDuration.TotalMilliseconds);

            return story;
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            var pipelineDuration = DateTime.UtcNow - pipelineStartTime;
            _logger.LogError(ex, "Error in story generation pipeline for file: {FileName} after {Duration}ms: {ErrorMessage}", 
                fileName, pipelineDuration.TotalMilliseconds, ex.Message);
            throw;
        }
    }

    public async Task<GeneratedStory?> GetStoryByIdAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Story ID is null or empty");
                throw new ArgumentNullException(nameof(id));
            }

            _logger.LogInformation("Retrieving story with ID: {StoryId}", id);

            if (_cache.TryGetValue(id, out GeneratedStory? story))
            {
                _logger.LogInformation("Story found in cache: {StoryId}", id);
                return story;
            }

            _logger.LogWarning("Story not found in cache: {StoryId}", id);
            return null;
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Error retrieving story with ID: {StoryId}: {ErrorMessage}", id, ex.Message);
            throw;
        }
    }

    public async Task<string> ExportStoryAsJsonAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Story ID is null or empty for JSON export");
                throw new ArgumentNullException(nameof(id));
            }

            _logger.LogInformation("Exporting story as JSON: {StoryId}", id);

            var story = await GetStoryByIdAsync(id);
            
            if (story == null)
            {
                _logger.LogWarning("Story not found for JSON export: {StoryId}", id);
                throw new StoryNotFoundException(id);
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(story, jsonOptions);

            _logger.LogInformation("Story exported as JSON successfully: {StoryId}, Size: {Size} bytes", 
                id, json.Length);
            return json;
        }
        catch (Exception ex) when (ex is not ArgumentNullException && ex is not StoryNotFoundException)
        {
            _logger.LogError(ex, "Error exporting story as JSON: {StoryId}: {ErrorMessage}", id, ex.Message);
            throw;
        }
    }

    public async Task<byte[]> ExportStoryAsPdfAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogError("Story ID is null or empty for PDF export");
                throw new ArgumentNullException(nameof(id));
            }

            _logger.LogInformation("Exporting story as PDF: {StoryId}", id);

            var story = await GetStoryByIdAsync(id);
            
            if (story == null)
            {
                _logger.LogWarning("Story not found for PDF export: {StoryId}", id);
                throw new StoryNotFoundException(id);
            }

            if (string.IsNullOrEmpty(story.PdfFilePath))
            {
                _logger.LogWarning("PDF file path not set for story {StoryId}, generating new PDF", id);
                // Generate PDF if not already generated
                var pdfBytes = await _pdfGenerator.GenerateStoryPdfAsync(story);
                var pdfFilePath = await _storageService.SaveStoryPdfAsync(story.Id, pdfBytes);
                story.PdfFilePath = pdfFilePath;
                StoreStoryInCache(story);
                _logger.LogInformation("New PDF generated and saved for story {StoryId}", id);
                return pdfBytes;
            }

            // Retrieve existing PDF from storage
            var storedPdf = await _storageService.GetStoryPdfAsync(story.PdfFilePath);

            _logger.LogInformation("Story PDF exported successfully: {StoryId}, Size: {Size} bytes", 
                id, storedPdf.Length);
            return storedPdf;
        }
        catch (Exception ex) when (ex is not ArgumentNullException && ex is not StoryNotFoundException)
        {
            _logger.LogError(ex, "Error exporting story as PDF: {StoryId}: {ErrorMessage}", id, ex.Message);
            throw;
        }
    }

    private string GenerateUniqueStoryId()
    {
        // Generate a unique ID using GUID
        var guid = Guid.NewGuid().ToString("N");
        _logger.LogDebug("Generated unique story ID: {StoryId}", guid);
        return guid;
    }

    private void StoreStoryInCache(GeneratedStory story)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheExpirationHours)
        };

        _cache.Set(story.Id, story, cacheOptions);
        _logger.LogDebug("Story stored in cache with {Hours} hour expiration: {StoryId}", 
            CacheExpirationHours, story.Id);
    }
}
