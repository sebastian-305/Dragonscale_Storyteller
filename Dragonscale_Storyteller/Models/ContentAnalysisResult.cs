using System.ComponentModel.DataAnnotations;

namespace Dragonscale_Storyteller.Models;

public class ContentAnalysisResult
{
    [Required]
    public List<string> KeyFacts { get; set; } = new();
    
    [Required]
    public List<string> Entities { get; set; } = new();
    
    [Required]
    public List<string> Concepts { get; set; } = new();
    
    [Required]
    [StringLength(2000)]
    public string OverallContext { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string SourceType { get; set; } = string.Empty;
}
