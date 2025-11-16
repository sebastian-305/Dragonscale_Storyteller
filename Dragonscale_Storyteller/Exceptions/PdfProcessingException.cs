namespace Dragonscale_Storyteller.Exceptions;

/// <summary>
/// Exception thrown when PDF processing fails
/// </summary>
public class PdfProcessingException : Exception
{
    public string? FileName { get; }
    public PdfProcessingErrorType ErrorType { get; }

    public PdfProcessingException(
        string message, 
        PdfProcessingErrorType errorType,
        string? fileName = null) 
        : base(message)
    {
        ErrorType = errorType;
        FileName = fileName;
    }

    public PdfProcessingException(
        string message, 
        Exception innerException,
        PdfProcessingErrorType errorType,
        string? fileName = null) 
        : base(message, innerException)
    {
        ErrorType = errorType;
        FileName = fileName;
    }
}

public enum PdfProcessingErrorType
{
    InvalidFormat,
    FileSizeExceeded,
    CorruptedFile,
    NoTextContent,
    ExtractionFailed
}
