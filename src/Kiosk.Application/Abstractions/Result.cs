namespace Kiosk.Application.Abstractions;

public record Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);

    public static Result Fail(Error error) => new(false, error);

    public static implicit operator bool(Result result) => result.IsSuccess;
}

public record Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, Error? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);

    public static new Result<T> Fail(Error error) => new(false, default, error);
}
