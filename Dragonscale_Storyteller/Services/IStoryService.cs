using Dragonscale_Storyteller.Models;

namespace Dragonscale_Storyteller.Services;

public interface IStoryService
{
    Task<GeneratedStory> CreateStoryFromPdfAsync(Stream pdfStream, string fileName, StoryConfiguration? config = null);
    Task<GeneratedStory?> GetStoryByIdAsync(string id);
    Task<string> ExportStoryAsJsonAsync(string id);
    Task<byte[]> ExportStoryAsPdfAsync(string id);
}
