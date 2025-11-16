using System.ComponentModel.DataAnnotations;

namespace Dragonscale_Storyteller.Models;

public class StoryResponse
{
    [Required]
    public bool Success { get; set; }
    
    public string? StoryId { get; set; }
    
    public GeneratedStory? Story { get; set; }
    
    public string? ErrorMessage { get; set; }
}
