namespace BillingFlow.Application.Common.Results;

/// <summary>
/// Defines the explicit contract for all Result types using C# 11 Static Abstract Members.
/// Allows MediatR pipeline behaviors to instantiate failures at compile-time without reflection.
/// </summary>
public interface IResult<TSelf> where TSelf : IResult<TSelf>
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error Error { get; }

    static abstract TSelf Failure(Error error);
    static abstract TSelf ValidationFailure(IDictionary<string, string[]> errors);
}
