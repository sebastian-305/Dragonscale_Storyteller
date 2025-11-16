namespace Dragonscale_Storyteller.Exceptions;

/// <summary>
/// Exception thrown when AI service operations fail
/// </summary>
public class AiServiceException : Exception
{
    public AiServiceErrorType ErrorType { get; }
    public string? RequestContext { get; }

    public AiServiceException(
        string message,
        AiServiceErrorType errorType,
        string? requestContext = null)
        : base(message)
    {
        ErrorType = errorType;
        RequestContext = requestContext;
    }

    public AiServiceException(
        string message,
        Exception innerException,
        AiServiceErrorType errorType,
        string? requestContext = null)
        : base(message, innerException)
    {
        ErrorType = errorType;
        RequestContext = requestContext;
    }
}

public enum AiServiceErrorType
{
    AuthenticationFailed,
    RateLimitExceeded,
    ServiceUnavailable,
    InvalidResponse,
    ContentAnalysisFailed,
    StoryGenerationFailed,
    ImagePromptGenerationFailed,
    ImageGenerationFailed
}
