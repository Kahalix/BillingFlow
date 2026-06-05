namespace BillingFlow.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a billing invoice.
/// </summary>
public enum InvoiceStatus
{
    Draft = 1,
    Unpaid = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Canceled = 5
}
