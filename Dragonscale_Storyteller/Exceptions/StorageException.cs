namespace Dragonscale_Storyteller.Exceptions;

/// <summary>
/// Exception thrown when storage operations fail
/// </summary>
public class StorageException : Exception
{
    public string? FilePath { get; }
    public StorageErrorType ErrorType { get; }

    public StorageException(
        string message,
        StorageErrorType errorType,
        string? filePath = null)
        : base(message)
    {
        ErrorType = errorType;
        FilePath = filePath;
    }

    public StorageException(
        string message,
        Exception innerException,
        StorageErrorType errorType,
        string? filePath = null)
        : base(message, innerException)
    {
        ErrorType = errorType;
        FilePath = filePath;
    }
}

public enum StorageErrorType
{
    FileNotFound,
    SaveFailed,
    ReadFailed,
    DirectoryCreationFailed
}
