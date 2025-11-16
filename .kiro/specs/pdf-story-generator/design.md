# Design Document: PDF Story Generator

## Overview

Das Dragonscale Storyteller System ist eine ASP.NET Core Webanwendung, die beliebige PDF-Dokumente in kreative, illustrierte Geschichten transformiert. Die Architektur folgt einem mehrschichtigen Ansatz mit klarer Trennung zwischen PDF-Verarbeitung, KI-gestützter Content-Analyse, Story-Generierung und Präsentation.

Das System nutzt:
- **PdfPig** für PDF-Textextraktion
- **OpenAI Client Library** für die Kommunikation mit Nebius AI
- **ASP.NET Core Web API** für Backend-Services
- **Static Web Frontend** für die Benutzeroberfläche

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Web Frontend                          │
│                    (HTML/CSS/JavaScript)                     │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP/JSON
┌────────────────────────┴────────────────────────────────────┐
│                    API Controller Layer                      │
│                  (StoryGeneratorController)                  │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                    Service Layer                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ PDF Service  │  │ Story Service│  │  AI Service  │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────┐
│                    External Services                         │
│              Nebius AI (via OpenAI Library)                  │
└─────────────────────────────────────────────────────────────┘
```

### Component Flow

1. **Upload Flow**: User → Web Frontend → API Controller → PDF Service → Text Extraction
2. **Analysis Flow**: Extracted Text → AI Service → Content Analysis → Structured Data
3. **Story Generation Flow**: Structured Data → Story Service → AI Service → Story Phases
4. **Prompt Generation Flow**: Story Phases → AI Service → Image Prompts
5. **Display Flow**: Complete Story → API Controller → Web Frontend → User

## Components and Interfaces

### 1. API Controller Layer

#### StoryGeneratorController
```csharp
[ApiController]
[Route("api/[controller]")]
public class StoryGeneratorController : ControllerBase
{
    // POST: api/storygenerator/upload
    Task<IActionResult> UploadPdf(IFormFile file);
    
    // GET: api/storygenerator/{id}
    Task<IActionResult> GetStory(string id);
    
    // GET: api/storygenerator/{id}/export/json
    Task<IActionResult> ExportStoryAsJson(string id);
    
    // GET: api/storygenerator/{id}/export/pdf
    Task<IActionResult> ExportStoryAsPdf(string id);
}
```

**Responsibilities:**
- Handle HTTP requests and responses
- Validate file uploads
- Coordinate service calls
- Return appropriate status codes and error messages
- Serve generated PDF files

### 2. Service Layer

#### IPdfProcessorService
```csharp
public interface IPdfProcessorService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    bool ValidatePdfFile(IFormFile file);
}
```

**Responsibilities:**
- PDF file validation (format, size)
- Text extraction using PdfPig
- Error handling for corrupted files

#### IAiService
```csharp
public interface IAiService
{
    Task<ContentAnalysisResult> AnalyzeContentAsync(string text);
    Task<StoryGenerationResult> GenerateStoryAsync(ContentAnalysisResult analysis);
    Task<string> GenerateImagePromptAsync(StoryPhase phase);
}
```

**Responsibilities:**
- Communication with Nebius AI via OpenAI library
- Content analysis and entity extraction
- Story generation with creative transformation
- Image prompt generation
- API authentication and error handling

#### IStoryService
```csharp
public interface IStoryService
{
    Task<GeneratedStory> CreateStoryFromPdfAsync(Stream pdfStream, string fileName);
    Task<GeneratedStory> GetStoryByIdAsync(string id);
    Task<string> ExportStoryAsJsonAsync(string id);
    Task<byte[]> ExportStoryAsPdfAsync(string id);
}
```

**Responsibilities:**
- Orchestrate the complete story generation pipeline
- Manage story state and storage
- Coordinate between PDF processing, AI analysis, and story synthesis
- Generate PDF output of the story

### 3. Configuration Service

#### NebiusAiConfiguration
```csharp
public class NebiusAiConfiguration
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public string TextModel { get; set; }
    public string ImageModel { get; set; }
}
```

**Configuration in appsettings.json:**
```json
{
  "NebiusAi": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://api.nebius.ai/v1",
    "TextModel": "meta-llama/Meta-Llama-3.1-8B-Instruct-fast",
    "ImageModel": "black-forest-labs/flux-schnell"
  }
}
```

## Data Models

### Core Domain Models

#### GeneratedStory
```csharp
public class GeneratedStory
{
    public string Id { get; set; }
    public string Title { get; set; }
    public List<StoryPhase> Phases { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SourceFileName { get; set; }
    public string PdfFilePath { get; set; } // Path to generated PDF on server
}
```

#### StoryPhase
```csharp
public class StoryPhase
{
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Mood { get; set; }
    public string ImagePrompt { get; set; }
    public int Order { get; set; }
}
```

#### ContentAnalysisResult
```csharp
public class ContentAnalysisResult
{
    public List<string> KeyFacts { get; set; }
    public List<string> Entities { get; set; }
    public List<string> Concepts { get; set; }
    public string OverallContext { get; set; }
    public string SourceType { get; set; } // e.g., "manual", "article", "list"
}
```

#### StoryGenerationResult
```csharp
public class StoryGenerationResult
{
    public string Title { get; set; }
    public List<StoryPhaseData> Phases { get; set; }
}

public class StoryPhaseData
{
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Mood { get; set; }
}
```

### API Response Models

#### StoryResponse
```csharp
public class StoryResponse
{
    public bool Success { get; set; }
    public string StoryId { get; set; }
    public GeneratedStory Story { get; set; }
    public string ErrorMessage { get; set; }
}
```

## AI Integration Design

### Nebius AI Service Implementation

The AI service uses the OpenAI client library configured for Nebius endpoints:

```csharp
public class NebiusAiService : IAiService
{
    private readonly OpenAIClient _client;
    private readonly NebiusAiConfiguration _config;
    
    public NebiusAiService(IOptions<NebiusAiConfiguration> config)
    {
        _config = config.Value;
        _client = new OpenAIClient(
            new ApiKeyCredential(_config.ApiKey),
            new OpenAIClientOptions 
            { 
                Endpoint = new Uri(_config.BaseUrl) 
            }
        );
    }
}
```

### AI Prompt Strategy

#### Content Analysis Prompt
```
Analyze the following text extracted from a document. 
Identify key facts, entities, concepts, and contextual information.
The document may be of any type (manual, article, list, etc.).
Extract elements that could be creatively transformed into narrative components.

Text: {extractedText}

Return a structured analysis with:
- Key facts and information
- Named entities (people, places, things, brands)
- Core concepts and themes
- Overall context and document type
```

#### Story Generation Prompt
```
Create a creative, engaging story based on the following analysis.
Transform the source material into a narrative with 4 distinct phases:
1. Introduction
2. Conflict
3. Climax
4. Resolution

Source Analysis: {contentAnalysis}

For each phase, provide:
- Phase name
- Detailed summary (2-3 sentences)
- Mood/atmosphere

Be creative and imaginative while incorporating elements from the source material.
```

#### Image Prompt Generation
```
Create a detailed image generation prompt for the following story phase.
The prompt should be suitable for AI image generation models.

Phase: {phaseName}
Summary: {phaseSummary}
Mood: {mood}

Generate a prompt that includes:
- Visual composition and framing
- Lighting and atmosphere
- Art style and mood
- Key visual elements

Format: Single paragraph, descriptive, specific
```

## Error Handling

### Error Categories

1. **PDF Processing Errors**
   - Invalid file format
   - Corrupted PDF
   - Empty or unreadable content
   - File size exceeded

2. **AI Service Errors**
   - API authentication failure
   - Rate limiting
   - Service unavailable
   - Invalid response format

3. **Story Generation Errors**
   - Insufficient content for story generation
   - Malformed AI response
   - Missing required story elements

### Error Response Strategy

```csharp
public class ErrorResponse
{
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public string UserFriendlyMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**Error Handling Principles:**
- Log all errors with context for debugging
- Return user-friendly error messages
- Use appropriate HTTP status codes
- Graceful degradation where possible
- Retry logic for transient failures

## Storage Strategy

### File System Storage (Initial Implementation)

For the initial implementation, use file system storage for generated PDFs and in-memory cache for story metadata:

```csharp
public class StoryStorageService
{
    private readonly IMemoryCache _cache;
    private readonly string _storageBasePath;
    
    public Task SaveStoryAsync(GeneratedStory story)
    {
        _cache.Set(story.Id, story, TimeSpan.FromHours(24));
    }
    
    public Task<GeneratedStory> GetStoryAsync(string id)
    {
        _cache.TryGetValue(id, out GeneratedStory story);
        return Task.FromResult(story);
    }
    
    public async Task<string> SaveStoryPdfAsync(string storyId, byte[] pdfContent)
    {
        var fileName = $"{storyId}.pdf";
        var filePath = Path.Combine(_storageBasePath, fileName);
        await File.WriteAllBytesAsync(filePath, pdfContent);
        return filePath;
    }
    
    public async Task<byte[]> GetStoryPdfAsync(string filePath)
    {
        return await File.ReadAllBytesAsync(filePath);
    }
}
```

**Storage Structure:**
```
/wwwroot/generated-stories/
  ├── {story-id-1}.pdf
  ├── {story-id-2}.pdf
  └── ...
```

**Considerations:**
- Stories expire from cache after 24 hours
- PDF files persist on disk
- Implement cleanup job for old PDFs
- Suitable for MVP and testing
- Can be replaced with database + blob storage later

## PDF Generation

### PDF Output Service

The system generates a formatted PDF document containing the complete story:

#### IPdfGeneratorService
```csharp
public interface IPdfGeneratorService
{
    Task<byte[]> GenerateStoryPdfAsync(GeneratedStory story);
}
```

**PDF Content Structure:**
1. Title page with story title
2. Table of contents with phase names
3. Each phase on separate page(s) with:
   - Phase name as heading
   - Summary text
   - Mood indicator
   - Image prompt in styled box
4. Footer with generation timestamp

**Implementation Options:**
- **QuestPDF**: Modern, fluent API for PDF generation (recommended)
- **iTextSharp**: Mature library with extensive features
- **PdfSharpCore**: Lightweight alternative

**PDF Styling:**
- Professional typography
- Consistent spacing and margins
- Color-coded phase sections
- Readable font sizes
- Page numbers

## Frontend Design

### Technology Stack
- HTML5
- CSS3 (with modern layout techniques)
- Vanilla JavaScript (ES6+)
- Fetch API for HTTP requests

### Key UI Components

#### 1. Upload Interface
- Drag-and-drop zone for PDF files
- File selection button
- Upload progress indicator
- File validation feedback

#### 2. Story Display
- Story title header
- Phase cards with:
  - Phase name badge
  - Summary text
  - Mood indicator
  - Image prompt display (copyable)
- Sequential layout with visual flow

#### 3. Export Controls
- Download JSON button
- Download PDF button (formatted story document)
- Copy to clipboard functionality
- Share options (future enhancement)

### 4. Story Customization (Future Enhancement)
- Dropdown for story genre/style selection
- Radio buttons for tone (serious, humorous, dramatic)
- Slider for story length/detail level
- Text field for additional instructions
- These options will be passed to the AI service to influence story generation

### API Integration

```javascript
// Upload PDF
async function uploadPdf(file) {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await fetch('/api/storygenerator/upload', {
        method: 'POST',
        body: formData
    });
    
    return await response.json();
}

// Get Story
async function getStory(storyId) {
    const response = await fetch(`/api/storygenerator/${storyId}`);
    return await response.json();
}

// Export Story as JSON
async function exportStoryAsJson(storyId) {
    const response = await fetch(`/api/storygenerator/${storyId}/export/json`);
    const blob = await response.blob();
    // Trigger download
}

// Export Story as PDF
async function exportStoryAsPdf(storyId) {
    const response = await fetch(`/api/storygenerator/${storyId}/export/pdf`);
    const blob = await response.blob();
    // Trigger download
}
```

## Testing Strategy

### Unit Testing

**Test Coverage Areas:**
1. PDF text extraction with various document types
2. AI service response parsing
3. Story phase generation logic
4. Error handling scenarios
5. Data model validation

**Testing Tools:**
- xUnit for test framework
- Moq for mocking dependencies
- FluentAssertions for readable assertions

### Integration Testing

**Test Scenarios:**
1. End-to-end story generation from PDF upload
2. Nebius AI service integration
3. API endpoint responses
4. Error handling across layers

### Manual Testing

**Test Cases:**
1. Upload various PDF types (manual, article, list, etc.)
2. Verify story creativity and coherence
3. Validate image prompts quality
4. Test error scenarios (corrupted files, API failures)
5. UI/UX flow testing

## Security Considerations

1. **File Upload Security**
   - Validate file types and extensions
   - Limit file size (e.g., 10MB max)
   - Scan for malicious content
   - Sanitize file names

2. **API Security**
   - Store API keys in configuration (not in code)
   - Use environment variables for sensitive data
   - Implement rate limiting
   - Add request validation

3. **Data Privacy**
   - Don't persist sensitive PDF content long-term
   - Clear temporary files after processing
   - Implement data retention policies

## Performance Considerations

1. **PDF Processing**
   - Stream large files instead of loading into memory
   - Implement timeout for long-running extractions
   - Consider async processing for large documents

2. **AI Service Calls**
   - Implement caching for repeated requests
   - Use connection pooling
   - Set appropriate timeouts
   - Handle rate limiting gracefully

3. **Frontend Performance**
   - Lazy load story phases
   - Implement loading states
   - Optimize asset delivery

## Deployment Considerations

1. **Configuration Management**
   - Use appsettings.json for development
   - Environment variables for production
   - Separate configs for different environments

2. **Monitoring**
   - Log AI service calls and response times
   - Track error rates
   - Monitor PDF processing success rates

3. **Scalability**
   - Stateless design for horizontal scaling
   - Consider queue-based processing for high load
   - Cache frequently accessed stories
