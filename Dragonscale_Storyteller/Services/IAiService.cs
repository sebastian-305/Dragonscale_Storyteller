using Dragonscale_Storyteller.Models;

namespace Dragonscale_Storyteller.Services;

public interface IAiService
{
    Task<ContentAnalysisResult> AnalyzeContentAsync(string text);
    Task<StoryGenerationResult> GenerateStoryAsync(ContentAnalysisResult analysis, StoryConfiguration? config = null);
    Task<string> GenerateImagePromptAsync(StoryPhase phase);
    Task<byte[]> GenerateImageAsync(string prompt);
}
