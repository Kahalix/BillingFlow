// File: src/BillingFlow.Application/Interfaces/IApplicationDbContext.cs
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<Client> Clients { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<ProvidedService> ProvidedServices { get; }
    DbSet<PaymentAttempt> PaymentAttempts {  get; }
    DbSet<Payment> Payments {  get; }
    DbSet<StripeEventLog> StripeEventLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
