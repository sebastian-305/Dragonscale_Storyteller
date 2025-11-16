using System.ComponentModel.DataAnnotations;

namespace Dragonscale_Storyteller.Models;

public class StoryConfiguration
{
    /// <summary>
    /// Language for the story (de = German, en = English)
    /// </summary>
    [Required]
    [RegularExpression("^(de|en)$")]
    public string Language { get; set; } = "de";
    
    /// <summary>
    /// Overall mood/tone of the story
    /// </summary>
    [Required]
    public string Mood { get; set; } = "neutral";
    
    /// <summary>
    /// Optional keywords to incorporate into the story
    /// </summary>
    public List<string> Keywords { get; set; } = new();
}

public static class StoryMood
{
    public const string Neutral = "neutral";
    public const string Adventure = "adventure";
    public const string Epic = "epic";
    public const string Happy = "happy";
    public const string Sad = "sad";
    public const string Horror = "horror";
    public const string Dramatic = "dramatic";
    public const string Romantic = "romantic";
    public const string Mysterious = "mysterious";
    public const string Inspirational = "inspirational";
    public const string Dark = "dark";
}
