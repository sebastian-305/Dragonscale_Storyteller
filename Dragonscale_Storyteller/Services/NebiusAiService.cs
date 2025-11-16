using Dragonscale_Storyteller.Configuration;
using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Exceptions;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace Dragonscale_Storyteller.Services;

public class NebiusAiService : IAiService
{
    private readonly ChatClient _chatClient;
    private readonly NebiusAiConfiguration _config;
    private readonly ILogger<NebiusAiService> _logger;

    public NebiusAiService(
        IOptions<NebiusAiConfiguration> config,
        ILogger<NebiusAiService> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Validate configuration
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException("Nebius AI API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_config.BaseUrl))
        {
            throw new InvalidOperationException("Nebius AI base URL is not configured.");
        }

        // Initialize OpenAI client with Nebius endpoint
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(_config.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_config.BaseUrl)
            }
        );

        _chatClient = openAiClient.GetChatClient(_config.TextModel);
    }

    public async Task<ContentAnalysisResult> AnalyzeContentAsync(string text)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting content analysis for text of length {Length}", text.Length);

            var prompt = $@"Analyze the following text extracted from a document. 
Identify key facts, entities, concepts, and contextual information.
The document may be of any type (manual, article, list, etc.).
Extract elements that could be creatively transformed into narrative components.

Text: {text}

Return a JSON object with the following structure:
{{
  ""keyFacts"": [""fact1"", ""fact2"", ...],
  ""entities"": [""entity1"", ""entity2"", ...],
  ""concepts"": [""concept1"", ""concept2"", ...],
  ""overallContext"": ""description of the overall context"",
  ""sourceType"": ""type of document (e.g., manual, article, list)""
}}

Respond ONLY with valid JSON, no additional text.";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an expert content analyzer. Always respond with valid JSON only."),
                new UserChatMessage(prompt)
            };

            var response = await _chatClient.CompleteChatAsync(chatMessages);
            var content = response.Value.Content[0].Text;

            _logger.LogDebug("Received analysis response: {Response}", content);

            // Parse JSON response
            var analysisResult = JsonSerializer.Deserialize<ContentAnalysisResult>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (analysisResult == null)
            {
                _logger.LogError("Failed to deserialize content analysis response");
                throw new AiServiceException(
                    "Failed to parse content analysis response",
                    AiServiceErrorType.InvalidResponse,
                    "Content analysis");
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Content analysis completed successfully in {Duration}ms", duration.TotalMilliseconds);
            
            return analysisResult;
        }
        catch (AiServiceException)
        {
            throw;
        }
        catch (ClientResultException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Nebius AI API error during content analysis after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            
            var errorType = ex.Status switch
            {
                401 or 403 => AiServiceErrorType.AuthenticationFailed,
                429 => AiServiceErrorType.RateLimitExceeded,
                503 => AiServiceErrorType.ServiceUnavailable,
                _ => AiServiceErrorType.ContentAnalysisFailed
            };
            
            throw new AiServiceException(
                $"AI service error: {ex.Message}",
                ex,
                errorType,
                "Content analysis");
        }
        catch (JsonException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to parse AI response as JSON after {Duration}ms", duration.TotalMilliseconds);
            throw new AiServiceException(
                "Failed to parse AI response. The response was not valid JSON.",
                ex,
                AiServiceErrorType.InvalidResponse,
                "Content analysis");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Unexpected error during content analysis after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            throw new AiServiceException(
                $"Unexpected error during content analysis: {ex.Message}",
                ex,
                AiServiceErrorType.ContentAnalysisFailed,
                "Content analysis");
        }
    }

    public async Task<StoryGenerationResult> GenerateStoryAsync(ContentAnalysisResult analysis, StoryConfiguration? config = null)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Use default configuration if none provided
            config ??= new StoryConfiguration();
            
            _logger.LogInformation("Starting story generation from analysis with config: Language={Language}, Mood={Mood}", 
                config.Language, config.Mood);

            var analysisJson = JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });

            // Build language instruction
            var languageInstruction = config.Language == "en" 
                ? "Write the story in English." 
                : "Schreibe die Geschichte auf Deutsch.";

            // Build mood instruction
            var moodInstruction = config.Mood switch
            {
                "adventure" => config.Language == "en"
                    ? "The story should be adventurous, exciting, and full of exploration and discovery with a sense of journey and wonder."
                    : "Die Geschichte soll abenteuerlich, aufregend und voller Entdeckungen sein mit einem Gefühl von Reise und Staunen.",
                "epic" => config.Language == "en"
                    ? "The story should be epic, grand, and heroic with larger-than-life characters and monumental events."
                    : "Die Geschichte soll episch, großartig und heroisch sein mit überdimensionalen Charakteren und monumentalen Ereignissen.",
                "happy" => config.Language == "en" 
                    ? "The story should be funny, lighthearted, and humorous with comedic elements." 
                    : "Die Geschichte soll lustig, heiter und humorvoll mit komödiantischen Elementen sein.",
                "sad" => config.Language == "en" 
                    ? "The story should be sad, melancholic, and emotionally touching." 
                    : "Die Geschichte soll traurig, melancholisch und emotional berührend sein.",
                "horror" => config.Language == "en" 
                    ? "The story should be scary, suspenseful, and create a sense of dread and horror." 
                    : "Die Geschichte soll gruselig, spannend sein und ein Gefühl von Angst und Horror erzeugen.",
                "dramatic" => config.Language == "en" 
                    ? "The story should be dramatic, intense, and emotionally powerful with high stakes." 
                    : "Die Geschichte soll dramatisch, intensiv und emotional kraftvoll mit hohen Einsätzen sein.",
                "romantic" => config.Language == "en"
                    ? "The story should be romantic, passionate, and emotionally intimate with themes of love and connection."
                    : "Die Geschichte soll romantisch, leidenschaftlich und emotional intim sein mit Themen von Liebe und Verbindung.",
                "mysterious" => config.Language == "en"
                    ? "The story should be mysterious, enigmatic, and intriguing with secrets to uncover and puzzles to solve."
                    : "Die Geschichte soll mysteriös, rätselhaft und faszinierend sein mit Geheimnissen zum Aufdecken und Rätseln zum Lösen.",
                "inspirational" => config.Language == "en"
                    ? "The story should be inspirational, uplifting, and motivational with themes of overcoming challenges and personal growth."
                    : "Die Geschichte soll inspirierend, erhebend und motivierend sein mit Themen des Überwindens von Herausforderungen und persönlichem Wachstum.",
                "dark" => config.Language == "en"
                    ? "The story should be dark, gritty, and somber with mature themes and a pessimistic or cynical tone."
                    : "Die Geschichte soll düster, rau und ernst sein mit reifen Themen und einem pessimistischen oder zynischen Ton.",
                _ => config.Language == "en" 
                    ? "The story should have a balanced, neutral tone." 
                    : "Die Geschichte soll einen ausgewogenen, neutralen Ton haben."
            };

            // Build keywords instruction
            var keywordsInstruction = config.Keywords.Count > 0
                ? (config.Language == "en"
                    ? $"Incorporate these keywords into the story: {string.Join(", ", config.Keywords)}"
                    : $"Baue diese Schlüsselwörter in die Geschichte ein: {string.Join(", ", config.Keywords)}")
                : "";

            var prompt = $@"Create a creative, engaging story based on the following analysis.
Transform the source material into a narrative with 4 distinct phases:
1. Introduction
2. Conflict
3. Climax
4. Resolution

IMPORTANT INSTRUCTIONS:
{languageInstruction}
{moodInstruction}
{keywordsInstruction}

Source Analysis:
{analysisJson}

For each phase, provide:
- Phase name (e.g., ""Introduction"", ""Conflict"", ""Climax"", ""Resolution"")
- Detailed summary (2-3 sentences)
- Mood/atmosphere that matches the overall story mood

Be creative and imaginative while incorporating elements from the source material.

Return a JSON object with the following structure:
{{
  ""title"": ""Creative Story Title"",
  ""phases"": [
    {{
      ""name"": ""Introduction"",
      ""summary"": ""Detailed summary of this phase..."",
      ""mood"": ""mysterious""
    }},
    {{
      ""name"": ""Conflict"",
      ""summary"": ""Detailed summary of this phase..."",
      ""mood"": ""tense""
    }},
    {{
      ""name"": ""Climax"",
      ""summary"": ""Detailed summary of this phase..."",
      ""mood"": ""dramatic""
    }},
    {{
      ""name"": ""Resolution"",
      ""summary"": ""Detailed summary of this phase..."",
      ""mood"": ""triumphant""
    }}
  ]
}}

Respond ONLY with valid JSON, no additional text.";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a creative storyteller. Always respond with valid JSON only."),
                new UserChatMessage(prompt)
            };

            var response = await _chatClient.CompleteChatAsync(chatMessages);
            var content = response.Value.Content[0].Text;

            _logger.LogDebug("Received story generation response: {Response}", content);

            // Parse JSON response
            var storyResult = JsonSerializer.Deserialize<StoryGenerationResult>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (storyResult == null || storyResult.Phases.Count < 4)
            {
                _logger.LogError("Failed to generate a complete story with 4 phases. Received {PhaseCount} phases", 
                    storyResult?.Phases.Count ?? 0);
                throw new AiServiceException(
                    "Failed to generate a complete story with 4 phases",
                    AiServiceErrorType.StoryGenerationFailed,
                    "Story generation");
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Story generation completed successfully with title: {Title} in {Duration}ms", 
                storyResult.Title, duration.TotalMilliseconds);
            
            return storyResult;
        }
        catch (AiServiceException)
        {
            throw;
        }
        catch (ClientResultException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Nebius AI API error during story generation after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            
            var errorType = ex.Status switch
            {
                401 or 403 => AiServiceErrorType.AuthenticationFailed,
                429 => AiServiceErrorType.RateLimitExceeded,
                503 => AiServiceErrorType.ServiceUnavailable,
                _ => AiServiceErrorType.StoryGenerationFailed
            };
            
            throw new AiServiceException(
                $"AI service error: {ex.Message}",
                ex,
                errorType,
                "Story generation");
        }
        catch (JsonException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to parse AI response as JSON after {Duration}ms", duration.TotalMilliseconds);
            throw new AiServiceException(
                "Failed to parse AI response. The response was not valid JSON.",
                ex,
                AiServiceErrorType.InvalidResponse,
                "Story generation");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Unexpected error during story generation after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            throw new AiServiceException(
                $"Unexpected error during story generation: {ex.Message}",
                ex,
                AiServiceErrorType.StoryGenerationFailed,
                "Story generation");
        }
    }

    public async Task<string> GenerateImagePromptAsync(StoryPhase phase)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Generating image prompt for phase: {PhaseName}", phase.Name);

            var prompt = $@"Create a detailed image generation prompt for the following story phase.
The prompt should be suitable for AI image generation models.

Phase: {phase.Name}
Summary: {phase.Summary}
Mood: {phase.Mood}

Generate a prompt that includes:
- Visual composition and framing
- Lighting and atmosphere
- Art style and mood
- Key visual elements

Format: Single paragraph, descriptive, specific.
Respond with ONLY the image prompt text, no additional formatting or explanation.";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an expert at creating detailed image generation prompts. Respond with only the prompt text."),
                new UserChatMessage(prompt)
            };

            var response = await _chatClient.CompleteChatAsync(chatMessages);
            var imagePrompt = response.Value.Content[0].Text.Trim();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Image prompt generated successfully for phase: {PhaseName} in {Duration}ms", 
                phase.Name, duration.TotalMilliseconds);
            _logger.LogDebug("Generated prompt: {Prompt}", imagePrompt);

            return imagePrompt;
        }
        catch (ClientResultException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Nebius AI API error during image prompt generation after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            
            var errorType = ex.Status switch
            {
                401 or 403 => AiServiceErrorType.AuthenticationFailed,
                429 => AiServiceErrorType.RateLimitExceeded,
                503 => AiServiceErrorType.ServiceUnavailable,
                _ => AiServiceErrorType.ImagePromptGenerationFailed
            };
            
            throw new AiServiceException(
                $"AI service error: {ex.Message}",
                ex,
                errorType,
                $"Image prompt generation for phase: {phase.Name}");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Unexpected error during image prompt generation after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            throw new AiServiceException(
                $"Unexpected error during image prompt generation: {ex.Message}",
                ex,
                AiServiceErrorType.ImagePromptGenerationFailed,
                $"Image prompt generation for phase: {phase.Name}");
        }
    }

    public async Task<byte[]> GenerateImageAsync(string prompt)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting image generation with prompt length: {Length}", prompt.Length);
            _logger.LogDebug("Image prompt: {Prompt}", prompt);

            // Create HTTP client for image generation API
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            httpClient.Timeout = TimeSpan.FromMinutes(2); // Image generation can take time

            // Prepare request for Nebius AI image generation
            var requestBody = new
            {
                model = _config.ImageModel,
                prompt = prompt,
                n = 1,
                size = "1024x1024",
                response_format = "b64_json"
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Make API call
            var imageEndpoint = $"{_config.BaseUrl.TrimEnd('/')}/images/generations";
            _logger.LogDebug("Calling image generation endpoint: {Endpoint}", imageEndpoint);

            var response = await httpClient.PostAsync(imageEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Image generation API error: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);

                var errorType = (int)response.StatusCode switch
                {
                    401 or 403 => AiServiceErrorType.AuthenticationFailed,
                    429 => AiServiceErrorType.RateLimitExceeded,
                    503 => AiServiceErrorType.ServiceUnavailable,
                    _ => AiServiceErrorType.ImageGenerationFailed
                };

                throw new AiServiceException(
                    $"Image generation failed with status {response.StatusCode}: {errorContent}",
                    errorType,
                    "Image generation");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received image generation response");

            // Parse response
            using var jsonDoc = JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray) || dataArray.GetArrayLength() == 0)
            {
                _logger.LogError("Invalid image generation response: no data array");
                throw new AiServiceException(
                    "Invalid response from image generation API: no image data",
                    AiServiceErrorType.InvalidResponse,
                    "Image generation");
            }

            var firstImage = dataArray[0];
            if (!firstImage.TryGetProperty("b64_json", out var b64JsonElement))
            {
                _logger.LogError("Invalid image generation response: no b64_json field");
                throw new AiServiceException(
                    "Invalid response from image generation API: no base64 image data",
                    AiServiceErrorType.InvalidResponse,
                    "Image generation");
            }

            var base64Image = b64JsonElement.GetString();
            if (string.IsNullOrEmpty(base64Image))
            {
                _logger.LogError("Empty base64 image data received");
                throw new AiServiceException(
                    "Empty image data received from API",
                    AiServiceErrorType.InvalidResponse,
                    "Image generation");
            }

            // Convert base64 to bytes
            var imageBytes = Convert.FromBase64String(base64Image);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Image generated successfully in {Duration}ms, size: {Size} bytes", 
                duration.TotalMilliseconds, imageBytes.Length);

            return imageBytes;
        }
        catch (AiServiceException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "HTTP error during image generation after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            
            throw new AiServiceException(
                $"Network error during image generation: {ex.Message}",
                ex,
                AiServiceErrorType.ServiceUnavailable,
                "Image generation");
        }
        catch (TaskCanceledException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Image generation timeout after {Duration}ms", duration.TotalMilliseconds);
            
            throw new AiServiceException(
                "Image generation timed out. Please try again.",
                ex,
                AiServiceErrorType.ServiceUnavailable,
                "Image generation");
        }
        catch (JsonException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to parse image generation response after {Duration}ms", duration.TotalMilliseconds);
            
            throw new AiServiceException(
                "Failed to parse image generation response",
                ex,
                AiServiceErrorType.InvalidResponse,
                "Image generation");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Unexpected error during image generation after {Duration}ms: {ErrorMessage}", 
                duration.TotalMilliseconds, ex.Message);
            
            throw new AiServiceException(
                $"Unexpected error during image generation: {ex.Message}",
                ex,
                AiServiceErrorType.ImageGenerationFailed,
                "Image generation");
        }
    }
}
