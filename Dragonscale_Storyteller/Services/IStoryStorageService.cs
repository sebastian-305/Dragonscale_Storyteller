namespace Dragonscale_Storyteller.Services;

public interface IStoryStorageService
{
    Task<string> SaveStoryPdfAsync(string storyId, byte[] pdfContent);
    Task<byte[]> GetStoryPdfAsync(string filePath);
}
