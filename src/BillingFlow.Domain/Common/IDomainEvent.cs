// File: src/BillingFlow.Domain/Common/IDomainEvent.cs
using MediatR;

namespace BillingFlow.Domain.Common;

/// <summary>
/// Represents a domain event that occurs within the domain model.
/// Implementing INotification allows seamless integration with MediatR for event dispatching.
/// </summary>
public interface IDomainEvent : INotification
{
}
