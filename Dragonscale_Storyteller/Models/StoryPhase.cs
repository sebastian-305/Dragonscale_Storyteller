using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dragonscale_Storyteller.Models;

public class StoryPhase
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
    
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string ImagePrompt { get; set; } = string.Empty;
    
    [Required]
    [Range(0, int.MaxValue)]
    public int Order { get; set; }
    
    /// <summary>
    /// Base64-encoded image data for this phase
    /// </summary>
    public string? ImageData { get; set; }
    
    /// <summary>
    /// File path to the saved image (for server-side storage)
    /// </summary>
    [JsonIgnore]
    public string? ImageFilePath { get; set; }
}
