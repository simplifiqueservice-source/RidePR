namespace RidePR.Shared.Results;

public class Result
{
    public bool Success { get; protected set; }

    public string Message { get; protected set; } = string.Empty;

    public static Result Ok(string message = "")
    {
        return new Result
        {
            Success = true,
            Message = message
        };
    }

    public static Result Fail(string message)
    {
        return new Result
        {
            Success = false,
            Message = message
        };
    }
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    public static Result<T> Ok(T data, string message = "")
    {
        return new Result<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public new static Result<T> Fail(string message)
    {
        return new Result<T>
        {
            Success = false,
            Message = message
        };
    }
}