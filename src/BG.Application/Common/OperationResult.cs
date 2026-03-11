namespace BG.Application.Common;

public sealed record OperationResult<T>(T? Value, string? ErrorCode)
{
    public bool Succeeded => string.IsNullOrWhiteSpace(ErrorCode);

    public static OperationResult<T> Success(T value)
    {
        return new OperationResult<T>(value, null);
    }

    public static OperationResult<T> Failure(string errorCode)
    {
        return new OperationResult<T>(default, errorCode);
    }
}
