namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstraction for dispatching system emails (e.g., password resets, invoices).
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default);
    Task SendEmailChangeConfirmationAsync(string newEmail, string confirmationToken, CancellationToken cancellationToken = default);
    Task SendEmailChangedNoticeAsync(string oldEmail, CancellationToken cancellationToken = default);
    Task SendClientSuspensionNoticeAsync(string toEmail, string companyName, CancellationToken cancellationToken = default);
    Task SendInvoiceOverdueNoticeAsync(string toEmail, string companyName, string invoiceNumber, decimal amountDue, CancellationToken cancellationToken = default);
}
