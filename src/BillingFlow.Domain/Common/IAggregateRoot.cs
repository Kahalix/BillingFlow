// File: src/BillingFlow.Domain/Common/IAggregateRoot.cs
namespace BillingFlow.Domain.Common;

/// <summary>
/// Marker interface used to denote an Aggregate Root in Domain-Driven Design.
/// Aggregate Roots are the only objects that external code should hold references to,
/// and they are responsible for ensuring the consistency of changes within their boundary.
/// </summary>
public interface IAggregateRoot
{
}
