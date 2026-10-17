using System;
using System.Collections.Generic;

namespace BillingFlow.Application.Common.Results;

public class Result : IResult<Result>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None) throw new InvalidOperationException();
        if (!isSuccess && error == Error.None) throw new InvalidOperationException();
        IsSuccess = isSuccess;
        Error = error;
    }

    // ---------------------------------------------------------
    // STANDARD FACTORY METHODS (For void Results)
    // ---------------------------------------------------------
    public static Result Success() => new(true, Error.None);
    
    public static Result Failure(Error error) => new(false, error);
    
    public static Result ValidationFailure(IDictionary<string, string[]> errors) 
        => new(false, new ValidationError("Validation.Failure", "One or more validation errors occurred.", errors));

    // ---------------------------------------------------------
    // GENERIC HELPER METHODS (For Result<TValue>)
    // Provides a unified API surface and enables Type Inference
    // ---------------------------------------------------------
    public static Result<TValue> Success<TValue>(TValue value) 
        => Result<TValue>.Success(value);
        
    public static Result<TValue> Failure<TValue>(Error error) 
        => Result<TValue>.Failure(error);
        
    public static Result<TValue> ValidationFailure<TValue>(IDictionary<string, string[]> errors) 
        => Result<TValue>.ValidationFailure(errors);
}

public class Result<TValue> : Result, IResult<Result<TValue>>
{
    private readonly TValue? _value;
    
    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);

    public static new Result<TValue> Failure(Error error) => new(default, false, error);
    
    public static new Result<TValue> ValidationFailure(IDictionary<string, string[]> errors) 
        => new(default, false, new ValidationError("Validation.Failure", "One or more validation errors occurred.", errors));
}
