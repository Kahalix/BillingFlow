namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstraction for dispatching system emails (e.g., password resets, invoices).
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(Guid outboxMessageId, string toEmail, string resetToken, CancellationToken cancellationToken = default);
    Task SendEmailChangeConfirmationAsync(Guid outboxMessageId, string newEmail, string confirmationToken, CancellationToken cancellationToken = default);
    Task SendEmailChangedNoticeAsync(Guid outboxMessageId, string oldEmail, CancellationToken cancellationToken = default);
    Task SendClientSuspensionNoticeAsync(Guid outboxMessageId, string toEmail, string companyName, CancellationToken cancellationToken = default);
    Task SendInvoiceOverdueNoticeAsync(Guid outboxMessageId, string toEmail, string companyName, string invoiceNumber, decimal amountDue, CancellationToken cancellationToken = default);
}
