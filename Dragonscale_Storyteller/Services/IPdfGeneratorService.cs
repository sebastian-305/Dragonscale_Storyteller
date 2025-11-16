namespace Dragonscale_Storyteller.Services;

using Dragonscale_Storyteller.Models;

public interface IPdfGeneratorService
{
    Task<byte[]> GenerateStoryPdfAsync(GeneratedStory story);
}
