using Dragonscale_Storyteller.Models;
using Dragonscale_Storyteller.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dragonscale_Storyteller.Services;

public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(ILogger<PdfGeneratorService> logger)
    {
        _logger = logger;
        
        // Configure QuestPDF license (Community license for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateStoryPdfAsync(GeneratedStory story)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (story == null)
            {
                _logger.LogError("Story object is null");
                throw new ArgumentNullException(nameof(story));
            }

            if (string.IsNullOrWhiteSpace(story.Id))
            {
                _logger.LogError("Story ID is null or empty");
                throw new ArgumentException("Story ID cannot be null or empty", nameof(story));
            }

            if (story.Phases == null || story.Phases.Count == 0)
            {
                _logger.LogError("Story has no phases: {StoryId}", story.Id);
                throw new ArgumentException("Story must have at least one phase", nameof(story));
            }

            _logger.LogInformation("Generating PDF for story: {StoryId}", story.Id);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .AlignCenter()
                        .Text(story.Title)
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Add each story phase (without metadata)
                            foreach (var phase in story.Phases.OrderBy(p => p.Order))
                            {
                                column.Item().Element(c => RenderPhase(c, phase));
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                        
                });
            });

            var pdfBytes = document.GeneratePdf();
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("PDF generated successfully for story: {StoryId}, Size: {Size} bytes, Duration: {Duration}ms", 
                story.Id, pdfBytes.Length, duration.TotalMilliseconds);

            return await Task.FromResult(pdfBytes);
        }
        catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error generating PDF for story: {StoryId} after {Duration}ms: {ErrorMessage}", 
                story?.Id ?? "unknown", duration.TotalMilliseconds, ex.Message);
            throw new StorageException(
                $"Failed to generate PDF: {ex.Message}",
                ex,
                StorageErrorType.SaveFailed,
                story?.Id);
        }
    }

    private void RenderPhase(IContainer container, StoryPhase phase)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Phase header (without badge, cleaner look)
            column.Item()
                .Text(phase.Name)
                .FontSize(18)
                .Bold()
                .FontColor(Colors.Blue.Darken2);

            // Generated image (if available)
            if (!string.IsNullOrEmpty(phase.ImageData))
            {
                try
                {
                    var imageBytes = Convert.FromBase64String(phase.ImageData);
                    column.Item()
                        .PaddingTop(10)
                        .MaxHeight(300)
                        .Image(imageBytes);
                    
                    _logger.LogDebug("Embedded image for phase: {PhaseName}", phase.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to embed image for phase: {PhaseName}", phase.Name);
                    // Continue without image
                }
            }

            // Summary
            column.Item()
                .PaddingTop(10)
                .Text(phase.Summary)
                .FontSize(12)
                .LineHeight(1.6f)
                .FontColor(Colors.Black);

            // Separator line between phases
            column.Item()
                .PaddingTop(20)
                .LineHorizontal(1)
                .LineColor(Colors.Grey.Lighten2);
        });
    }

    private string GetPhaseColor(int order)
    {
        return order switch
        {
            0 => Colors.Green.Medium,
            1 => Colors.Orange.Medium,
            2 => Colors.Red.Medium,
            3 => Colors.Blue.Medium,
            _ => Colors.Grey.Medium
        };
    }
}
