# Implementation Plan

- [x] 1. Set up configuration and dependency injection





  - Configure Nebius AI settings in appsettings.json with correct model names
  - Register services in Program.cs (IPdfProcessorService, IAiService, IStoryService, IPdfGeneratorService)
  - Set up IMemoryCache for story storage
  - Configure file storage path for generated PDFs
  - _Requirements: 7.1, 7.2, 7.3_

- [x] 2. Implement data models and DTOs





  - Create GeneratedStory, StoryPhase, ContentAnalysisResult, StoryGenerationResult models
  - Create API response models (StoryResponse, ErrorResponse)
  - Add data annotations for validation
  - _Requirements: 5.5_

- [x] 3. Implement PDF processing service




- [x] 3.1 Create PdfProcessorService with PdfPig integration


  - Implement ValidatePdfFile method (check format, size limits)
  - Implement ExtractTextFromPdfAsync using PdfPig library
  - Add error handling for corrupted or unreadable PDFs
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 3.2 Write unit tests for PDF processing


  - Test text extraction with sample PDFs
  - Test validation logic for various file types
  - Test error handling for corrupted files
  - _Requirements: 1.1, 1.2, 1.4_

- [x] 4. Implement Nebius AI service




- [x] 4.1 Create NebiusAiService with OpenAI client


  - Initialize OpenAI client with Nebius endpoint and API key
  - Implement AnalyzeContentAsync method with appropriate prompt
  - Implement GenerateStoryAsync method for story phase generation
  - Implement GenerateImagePromptAsync for each phase
  - Add error handling for API failures and rate limiting
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 7.1, 7.3, 7.4, 7.5_

- [x] 4.2 Write unit tests for AI service


  - Mock OpenAI client responses
  - Test content analysis parsing
  - Test story generation logic
  - Test error handling scenarios
  - _Requirements: 2.1, 3.1, 4.1, 7.4_

- [x] 5. Implement story orchestration service




- [x] 5.1 Create StoryService to coordinate the pipeline


  - Implement CreateStoryFromPdfAsync orchestrating PDF → Analysis → Story → Prompts
  - Generate unique story IDs
  - Store stories in memory cache
  - Implement GetStoryByIdAsync for retrieval
  - _Requirements: 1.5, 2.5, 3.1, 4.5, 5.1, 5.2, 5.3_

- [x] 5.2 Implement JSON export functionality


  - Create ExportStoryAsJsonAsync method
  - Format story data as structured JSON
  - Include all phases and metadata
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 5.3 Write integration tests for story service


  - Test end-to-end story generation flow
  - Test story retrieval
  - Test JSON export
  - _Requirements: 1.5, 5.1, 6.1_

- [x] 6. Implement PDF generation service




- [x] 6.1 Add QuestPDF package and create PdfGeneratorService


  - Install QuestPDF NuGet package
  - Implement GenerateStoryPdfAsync method
  - Create PDF layout with title page, phases, and styling
  - Format image prompts in styled boxes
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 6.2 Implement PDF storage functionality


  - Create StoryStorageService for file system operations
  - Implement SaveStoryPdfAsync to save PDFs to wwwroot/generated-stories
  - Implement GetStoryPdfAsync to retrieve stored PDFs
  - Update StoryService to save PDF after generation
  - _Requirements: 6.1, 6.4_

- [x] 6.3 Write unit tests for PDF generation


  - Test PDF creation with sample story data
  - Verify PDF structure and content
  - Test file storage operations
  - _Requirements: 6.1, 6.2_

- [-] 7. Implement API controller


- [x] 7.1 Create StoryGeneratorController with endpoints


  - Implement POST /api/storygenerator/upload endpoint
  - Implement GET /api/storygenerator/{id} endpoint
  - Implement GET /api/storygenerator/{id}/export/json endpoint
  - Implement GET /api/storygenerator/{id}/export/pdf endpoint
  - Add file upload validation and error handling
  - Return appropriate HTTP status codes
  - _Requirements: 1.1, 1.2, 1.4, 5.1, 5.2, 5.3, 6.1, 6.5, 8.1, 8.2, 8.5_

- [x] 7.2 Write integration tests for API endpoints


  - Test upload endpoint with various PDF files
  - Test story retrieval endpoint
  - Test export endpoints (JSON and PDF)
  - Test error responses
  - _Requirements: 1.1, 5.1, 6.1, 8.1, 8.2_

- [x] 8. Implement frontend UI





- [x] 8.1 Create HTML structure for upload and display


  - Create index.html with upload zone and story display area
  - Add drag-and-drop file upload interface
  - Create story phase card templates
  - Add export buttons (JSON and PDF)
  - _Requirements: 1.1, 5.1, 5.2, 5.3, 5.4, 6.1_


- [x] 8.2 Implement CSS styling

  - Style upload interface with drag-and-drop visual feedback
  - Style story phase cards with distinct visual sections
  - Add responsive layout for different screen sizes
  - Style loading states and error messages
  - _Requirements: 5.4, 8.5_

- [x] 8.3 Implement JavaScript functionality


  - Implement file upload with Fetch API
  - Add upload progress indicator
  - Implement story display rendering
  - Add JSON export download functionality
  - Add PDF export download functionality
  - Implement copy-to-clipboard for image prompts
  - Add error handling and user feedback
  - _Requirements: 1.1, 1.2, 1.4, 5.1, 5.2, 5.3, 6.1, 8.1, 8.2, 8.5_

- [x] 8.4 Test frontend functionality manually


  - Test file upload with various PDF types
  - Verify story display rendering
  - Test export functionality (JSON and PDF)
  - Test error scenarios
  - Verify responsive design
  - _Requirements: 1.1, 5.1, 6.1, 8.5_

- [x] 9. Implement error handling and logging




- [x] 9.1 Add comprehensive error handling


  - Add try-catch blocks in all service methods
  - Create custom exception types for different error scenarios
  - Implement error logging with context information
  - Return user-friendly error messages
  - _Requirements: 1.4, 2.5, 7.4, 8.1, 8.2, 8.3, 8.4, 8.5_


- [x] 9.2 Configure logging infrastructure

  - Configure logging in Program.cs
  - Add structured logging for AI service calls
  - Log PDF processing operations
  - Add performance metrics logging
  - _Requirements: 8.3_

- [x] 10. Final integration and testing





- [x] 10.1 Test complete end-to-end workflow


  - Upload various document types (manual, article, list, etc.)
  - Verify story generation quality and creativity
  - Test PDF generation and storage
  - Verify all export formats work correctly
  - Test error scenarios across the entire pipeline
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 6.1, 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4, 8.5_


- [x] 10.2 Verify Nebius AI integration

  - Confirm correct model names are used (Meta-Llama-3.1-8B-Instruct-fast, flux-schnell)
  - Test API authentication and error handling
  - Verify story quality with different input types
  - Test image prompt generation quality
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
