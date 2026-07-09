namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstracts ambient HTTP metadata.
/// Keeps CQRS commands and handlers decoupled from ASP.NET Core infrastructure.
/// </summary>
public interface IRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}
