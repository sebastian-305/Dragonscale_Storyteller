using System.ComponentModel.DataAnnotations;

namespace Dragonscale_Storyteller.Models;

public class ErrorResponse
{
    [Required]
    [StringLength(50)]
    public string ErrorCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string UserFriendlyMessage { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; }
}
