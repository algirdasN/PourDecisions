using System.Diagnostics.CodeAnalysis;

namespace PourDecisions.Application.Models;

public class Result
{
    protected Result()
    {
        IsSuccess = true;
        ErrorMessage = null;
    }

    protected Result(string errorMessage)
    {
        IsSuccess = false;
        ErrorMessage = errorMessage;
    }

    public string? ErrorMessage { get; }

    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    public virtual bool IsSuccess { get; }

    public static Result Success()
    {
        return new Result();
    }

    public static Result Error(string errorMessage)
    {
        return new Result(errorMessage);
    }
}

public class Result<T> : Result where T : notnull
{
    private Result(T value) : base()
    {
        Value = value;
    }

    private Result(string errorMessage) : base(errorMessage)
    {
    }

    public T? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public override bool IsSuccess => base.IsSuccess && Value is not null;

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public new static Result<T> Error(string errorMessage)
    {
        return new Result<T>(errorMessage);
    }
}
