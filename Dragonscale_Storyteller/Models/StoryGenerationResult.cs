using System.ComponentModel.DataAnnotations;

namespace Dragonscale_Storyteller.Models;

public class StoryGenerationResult
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MinLength(1)]
    public List<StoryPhaseData> Phases { get; set; } = new();
}

public class StoryPhaseData
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Summary { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Mood { get; set; } = string.Empty;
}
