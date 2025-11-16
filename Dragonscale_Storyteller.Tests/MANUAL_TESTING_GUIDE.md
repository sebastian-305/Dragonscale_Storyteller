# Manual Testing Guide - Dragonscale Storyteller

This guide provides comprehensive manual testing procedures for the complete end-to-end workflow of the Dragonscale Storyteller application.

## Prerequisites

1. **Nebius AI API Key**: Ensure you have a valid API key configured in `appsettings.json`
2. **Test PDF Files**: Prepare various types of PDF documents for testing
3. **Running Application**: Start the application using `dotnet run` or Visual Studio

## Test Scenarios

### 1. Technical Manual Test

**Objective**: Verify that technical documentation is transformed into a creative story

**Steps**:
1. Create or obtain a technical manual PDF (e.g., appliance manual, software documentation)
2. Navigate to the application homepage
3. Upload the PDF file via drag-and-drop or file selection
4. Wait for processing (observe progress indicator)
5. Verify the generated story

**Expected Results**:
- ✓ Story title is creative and relates to the source material
- ✓ Four distinct phases are generated (Introduction, Conflict, Climax, Resolution)
- ✓ Each phase has a name, summary, and mood
- ✓ Image prompts are descriptive and match the phase content
- ✓ Story demonstrates creativity while incorporating source elements

**Example Source**: Coffee maker manual
**Example Expected Output**: A story about a magical brewing device or adventure involving coffee

---

### 2. News Article Test

**Objective**: Verify that news articles are transformed into narrative stories

**Steps**:
1. Create or obtain a news article PDF
2. Upload the PDF through the web interface
3. Review the generated story

**Expected Results**:
- ✓ News facts are woven into a narrative structure
- ✓ Story has dramatic arc (introduction → conflict → climax → resolution)
- ✓ Mood changes appropriately across phases
- ✓ Image prompts capture key moments

**Example Source**: Local park renovation article
**Example Expected Output**: A story about community transformation or nature's revival

---

### 3. Shopping List Test

**Objective**: Verify that mundane lists are transformed creatively

**Steps**:
1. Create a simple shopping list PDF
2. Upload and process
3. Verify creative transformation

**Expected Results**:
- ✓ Mundane items are transformed into story elements
- ✓ Story is coherent despite simple source material
- ✓ Creativity is evident in the transformation
- ✓ All four phases are generated

**Example Source**: Grocery shopping list
**Example Expected Output**: A culinary adventure or quest for ingredients

---

### 4. PDF Generation and Storage Test

**Objective**: Verify PDF export functionality

**Steps**:
1. Generate a story from any PDF
2. Click "PDF exportieren" button
3. Verify the downloaded PDF file
4. Check server storage location

**Expected Results**:
- ✓ PDF downloads successfully
- ✓ PDF contains story title
- ✓ All four phases are included
- ✓ Image prompts are formatted in styled boxes
- ✓ PDF is readable and well-formatted
- ✓ File is saved in `wwwroot/generated-stories/` directory

**Verification**:
```bash
# Check generated PDFs
ls wwwroot/generated-stories/
```

---

### 5. JSON Export Test

**Objective**: Verify JSON export functionality

**Steps**:
1. Generate a story
2. Click "JSON exportieren" button
3. Open the downloaded JSON file

**Expected Results**:
- ✓ JSON file downloads successfully
- ✓ JSON is valid and well-formatted
- ✓ Contains all story data:
  - Story ID
  - Title
  - All phases with names, summaries, moods
  - Image prompts
  - Metadata (creation date, source filename)

**Sample JSON Structure**:
```json
{
  "id": "abc123",
  "title": "The Great Coffee Adventure",
  "phases": [
    {
      "name": "Introduction",
      "summary": "...",
      "mood": "mysterious",
      "imagePrompt": "...",
      "order": 1
    }
  ],
  "createdAt": "2024-01-01T00:00:00Z",
  "sourceFileName": "manual.pdf"
}
```

---

### 6. Story Retrieval Test

**Objective**: Verify story can be retrieved by ID

**Steps**:
1. Generate a story and note the story ID
2. Use browser developer tools or API client
3. Make GET request to `/api/storygenerator/{id}`

**Expected Results**:
- ✓ Story is retrieved successfully
- ✓ All data matches the original generation
- ✓ Response is properly formatted

**API Test**:
```bash
# Using curl (replace {id} with actual story ID)
curl http://localhost:5000/api/storygenerator/{id}
```

---

### 7. Error Handling Tests

#### 7.1 Invalid File Format

**Steps**:
1. Try to upload a non-PDF file (e.g., .txt, .docx, .jpg)

**Expected Results**:
- ✓ Error message displayed: "Bitte wählen Sie eine gültige PDF-Datei aus"
- ✓ Upload is rejected
- ✓ Application remains functional

#### 7.2 File Size Exceeded

**Steps**:
1. Try to upload a PDF larger than 10MB

**Expected Results**:
- ✓ Error message displayed: "Die Datei ist zu groß. Maximale Größe: 10MB"
- ✓ Upload is rejected

#### 7.3 Corrupted PDF

**Steps**:
1. Create a corrupted PDF file (rename a .txt file to .pdf)
2. Try to upload

**Expected Results**:
- ✓ Error message displayed about corrupted file
- ✓ Application handles error gracefully
- ✓ No server crash

#### 7.4 Empty PDF

**Steps**:
1. Create a PDF with no text content
2. Upload the file

**Expected Results**:
- ✓ Error message about no extractable content
- ✓ Graceful error handling

#### 7.5 Non-existent Story ID

**Steps**:
1. Try to retrieve a story with invalid ID
2. Make GET request to `/api/storygenerator/invalid-id`

**Expected Results**:
- ✓ 404 Not Found response
- ✓ User-friendly error message
- ✓ Error response includes proper structure

---

### 8. Frontend UI/UX Tests

#### 8.1 Drag and Drop

**Steps**:
1. Drag a PDF file over the drop zone
2. Observe visual feedback
3. Drop the file

**Expected Results**:
- ✓ Drop zone highlights when file is dragged over
- ✓ Visual feedback is clear
- ✓ File uploads successfully after drop

#### 8.2 Progress Indicator

**Steps**:
1. Upload a PDF file
2. Observe the progress bar

**Expected Results**:
- ✓ Progress bar appears immediately
- ✓ Progress updates during upload
- ✓ Status text changes ("Wird hochgeladen..." → "Geschichte wird generiert..." → "Fertig!")
- ✓ Progress bar reaches 100%

#### 8.3 Copy to Clipboard

**Steps**:
1. Generate a story
2. Click the copy button next to an image prompt

**Expected Results**:
- ✓ Image prompt is copied to clipboard
- ✓ Button shows visual feedback (changes color)
- ✓ Tooltip changes to "Kopiert!"
- ✓ Feedback resets after 2 seconds

#### 8.4 New Story Button

**Steps**:
1. Generate a story
2. Click "Neue Geschichte erstellen"

**Expected Results**:
- ✓ Story section is hidden
- ✓ Upload section is shown
- ✓ Page scrolls to top
- ✓ Previous story data is cleared

#### 8.5 Responsive Design

**Steps**:
1. Test on different screen sizes:
   - Desktop (1920x1080)
   - Tablet (768x1024)
   - Mobile (375x667)

**Expected Results**:
- ✓ Layout adapts to screen size
- ✓ All elements remain accessible
- ✓ Text is readable
- ✓ Buttons are clickable
- ✓ No horizontal scrolling

---

### 9. Nebius AI Integration Tests

#### 9.1 Model Configuration

**Steps**:
1. Check `appsettings.json`
2. Verify model names

**Expected Configuration**:
```json
{
  "NebiusAi": {
    "TextModel": "meta-llama/Meta-Llama-3.1-8B-Instruct-fast",
    "ImageModel": "black-forest-labs/flux-schnell"
  }
}
```

**Verification**:
- ✓ Text model is correctly configured
- ✓ Image model is correctly configured
- ✓ Base URL points to Nebius endpoint

#### 9.2 API Authentication

**Steps**:
1. Remove or invalidate API key
2. Try to generate a story

**Expected Results**:
- ✓ Authentication error is caught
- ✓ User-friendly error message displayed
- ✓ Error is logged
- ✓ Application doesn't crash

#### 9.3 Rate Limiting

**Steps**:
1. Generate multiple stories in quick succession
2. Observe behavior if rate limit is hit

**Expected Results**:
- ✓ Rate limit error is handled gracefully
- ✓ User is informed to wait
- ✓ Application remains functional

#### 9.4 Service Unavailability

**Steps**:
1. Simulate service unavailability (disconnect network or use invalid endpoint)
2. Try to generate a story

**Expected Results**:
- ✓ Service unavailable error is caught
- ✓ User-friendly message displayed
- ✓ Application remains stable

---

### 10. Story Quality Verification

#### 10.1 Creativity Assessment

**Criteria**:
- Story demonstrates creative interpretation of source material
- Narrative has clear beginning, middle, and end
- Phases flow logically from one to another
- Mood changes appropriately across phases

#### 10.2 Image Prompt Quality

**Criteria**:
- Prompts are descriptive and detailed
- Include visual composition, lighting, atmosphere
- Suitable for AI image generation
- Match the phase content and mood

#### 10.3 Consistency

**Criteria**:
- Story maintains thematic consistency
- Elements from source material are incorporated
- Tone is appropriate throughout
- No contradictions between phases

---

## Performance Benchmarks

### Expected Processing Times

| Document Type | Size | Expected Time |
|--------------|------|---------------|
| Simple list | < 1 page | 10-20 seconds |
| Article | 2-3 pages | 20-40 seconds |
| Manual | 5-10 pages | 40-60 seconds |

**Note**: Times may vary based on:
- Nebius AI API response time
- Network latency
- Document complexity

---

## Logging and Debugging

### Check Application Logs

```bash
# View console output for detailed logging
# Look for:
# - PDF processing logs
# - AI service call logs
# - Error messages with context
# - Performance metrics
```

### Key Log Messages to Monitor

1. **PDF Processing**:
   - "Received PDF upload request"
   - "PDF text extracted successfully"
   - "PDF processing error"

2. **AI Service**:
   - "Starting content analysis"
   - "Content analysis completed"
   - "Starting story generation"
   - "Story generation completed"
   - "Generating image prompt"

3. **Storage**:
   - "Story saved to cache"
   - "PDF saved to storage"

---

## Test Checklist

Use this checklist to track testing progress:

### Core Functionality
- [ ] Upload PDF via file selection
- [ ] Upload PDF via drag-and-drop
- [ ] Process technical manual
- [ ] Process news article
- [ ] Process shopping list
- [ ] Generate story with 4 phases
- [ ] Retrieve story by ID
- [ ] Export story as JSON
- [ ] Export story as PDF

### Error Handling
- [ ] Invalid file format rejection
- [ ] File size limit enforcement
- [ ] Corrupted PDF handling
- [ ] Empty PDF handling
- [ ] Non-existent story ID handling
- [ ] AI service error handling
- [ ] Network error handling

### UI/UX
- [ ] Drag-and-drop visual feedback
- [ ] Progress indicator updates
- [ ] Copy to clipboard functionality
- [ ] New story button resets state
- [ ] Responsive design on desktop
- [ ] Responsive design on tablet
- [ ] Responsive design on mobile
- [ ] Error messages display correctly

### Integration
- [ ] Nebius AI authentication works
- [ ] Correct model names used
- [ ] API calls succeed
- [ ] Rate limiting handled
- [ ] Service unavailability handled

### Quality
- [ ] Story creativity verified
- [ ] Image prompts are detailed
- [ ] Narrative flow is logical
- [ ] Mood changes appropriately
- [ ] Source material incorporated

---

## Troubleshooting

### Common Issues

**Issue**: "AI service authentication failed"
**Solution**: Check API key in `appsettings.json`

**Issue**: "Story not found"
**Solution**: Stories expire after 24 hours from cache

**Issue**: "PDF processing failed"
**Solution**: Ensure PDF is not password-protected or corrupted

**Issue**: Slow processing
**Solution**: Check network connection and Nebius AI service status

---

## Test Results Documentation

After completing tests, document results:

```
Test Date: _______________
Tester: _______________

Passed Tests: ___ / ___
Failed Tests: ___ / ___

Critical Issues Found:
1. _______________
2. _______________

Notes:
_______________
_______________
```

---

## Conclusion

This manual testing guide covers all aspects of the Dragonscale Storyteller application. Complete all test scenarios to ensure the system works correctly end-to-end.

For automated testing, refer to `EndToEndIntegrationTests.cs`.
