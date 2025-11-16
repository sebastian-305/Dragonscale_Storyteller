using Microsoft.AspNetCore.Http;

namespace Dragonscale_Storyteller.Services;

public interface IPdfProcessorService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    bool ValidatePdfFile(IFormFile file);
}
