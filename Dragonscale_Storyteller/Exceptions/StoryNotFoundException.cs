namespace Dragonscale_Storyteller.Exceptions;

/// <summary>
/// Exception thrown when a requested story cannot be found
/// </summary>
public class StoryNotFoundException : Exception
{
    public string StoryId { get; }

    public StoryNotFoundException(string storyId)
        : base($"Story with ID '{storyId}' was not found")
    {
        StoryId = storyId;
    }

    public StoryNotFoundException(string storyId, string message)
        : base(message)
    {
        StoryId = storyId;
    }
}
