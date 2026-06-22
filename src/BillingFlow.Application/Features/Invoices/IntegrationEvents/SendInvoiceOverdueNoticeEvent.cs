using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Invoices.IntegrationEvents;

/// <summary>
/// An integration event representing the intent to send an overdue invoice notice to a client.
/// Persisted durably within the Outbox log for asynchronous, resilient processing.
/// </summary>
public record SendInvoiceOverdueNoticeEvent(
    string Email,
    string CompanyName,
    string InvoiceNumber,
    decimal AmountDue) : IIntegrationEvent;
