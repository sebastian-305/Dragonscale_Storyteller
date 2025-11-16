using System.ComponentModel.DataAnnotations;

namespace Dragonscale_Storyteller.Models;

public class GeneratedStory
{
    [Required]
    public string Id { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MinLength(1)]
    public List<StoryPhase> Phases { get; set; } = new();
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string SourceFileName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? PdfFilePath { get; set; }
}
