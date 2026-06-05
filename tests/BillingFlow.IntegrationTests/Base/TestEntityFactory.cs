using System;
using System.Threading.Tasks;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.ValueObjects;
using BillingFlow.Infrastructure.Database;

using Microsoft.Extensions.DependencyInjection;

namespace BillingFlow.IntegrationTests.Base;

/// <summary>
/// A centralized Test Data Builder / Factory for generating Domain Entities.
/// Guarantees unique default values to prevent Unique Constraint collisions in the database.
/// Provides rich, optional parameters to strictly control the domain state for edge-case testing.
/// </summary>
public class TestEntityFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TestEntityFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a complete, linked hierarchy (AppUser + Client).
    /// Uses dynamic GUIDs to ensure unique emails and tax IDs unless explicitly overridden.
    /// </summary>
    public async Task<(AppUser User, Client Client)> CreateUserWithClientAsync(
        string? email = null,
        string? companyName = null,
        string? taxId = null,
        Role role = Role.Customer,
        DateTimeOffset? createdAt = null)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        email ??= $"user_{uniqueSuffix}@tech.com";
        companyName ??= $"Corp_{uniqueSuffix}";
        taxId ??= $"TAX-{uniqueSuffix}";
        var time = createdAt ?? DateTimeOffset.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        // 1. Create and persist User (Parent)
        var user = new AppUser(email, "dummy_hash", role, time);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 2. Create and persist Client (Child)
        // Note: If Client entity gets a Status parameter or method (e.g. Suspend()), 
        // it can be conditionally triggered here via a clientStatus parameter.
        var client = Client.Create(user.Id, companyName, taxId, new Address("St", "City", "Zip", "USA"));

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        return (user, client);
    }

    /// <summary>
    /// Creates and persists a billable service for a given client.
    /// Acts as the foundational data for testing automated invoice generation.
    /// </summary>
    public async Task<ProvidedService> CreateProvidedServiceAsync(
        Client client,
        string? description = null,
        decimal amount = 500m,
        DateTimeOffset? performedAt = null,
        DateTimeOffset? now = null)
    {
        description ??= $"IT_Support_{Guid.NewGuid().ToString("N")[..6]}";

        var currentTime = now ?? DateTimeOffset.UtcNow;
        var serviceTime = performedAt ?? currentTime.AddDays(-1); // Default to a past service

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var service = ProvidedService.Create(client.Id, description, amount, serviceTime, currentTime);

        db.Add(service);
        await db.SaveChangesAsync();

        return service;
    }

    /// <summary>
    /// Creates a User, Client, and a pending Provided Service in one shot.
    /// </summary>
    public async Task<(AppUser User, Client Client, ProvidedService Service)> CreateUserWithClientAndServiceAsync(
        decimal serviceAmount = 500m,
        DateTimeOffset? performedAt = null)
    {
        var (user, client) = await CreateUserWithClientAsync();
        var service = await CreateProvidedServiceAsync(client, amount: serviceAmount, performedAt: performedAt);

        return (user, client, service);
    }

    /// <summary>
    /// Creates and persists a standard Draft Invoice.
    /// </summary>
    public async Task<Invoice> CreateInvoiceAsync(
        Client client,
        string? invoiceNumber = null,
        DateTimeOffset? issueDate = null)
    {
        invoiceNumber ??= $"INV-{Guid.NewGuid().ToString("N")[..6]}";

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var invoice = Invoice.Create(client.Id, client.UserId, invoiceNumber, issueDate ?? DateTimeOffset.UtcNow);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return invoice;
    }

    /// <summary>
    /// Creates and persists an Issued (Unpaid) Invoice with a standard line item.
    /// </summary>
    public async Task<Invoice> CreateIssuedInvoiceAsync(
        Client client,
        string? invoiceNumber = null,
        decimal amount = 1000m,
        DateTimeOffset? issueDate = null)
    {
        invoiceNumber ??= $"INV-ISSUED-{Guid.NewGuid().ToString("N")[..6]}";

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var invoice = Invoice.Create(client.Id, client.UserId, invoiceNumber, issueDate ?? DateTimeOffset.UtcNow);
        invoice.AddLineItem("Standard Consulting Service", amount, 1);
        invoice.Issue(); // Translates to domain status 'Unpaid'

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return invoice;
    }

    /// <summary>
    /// Ultimate convenience helper for the most common Invoice retrieval tests.
    /// Sets up the User, Client, and an Issued Invoice all at once.
    /// </summary>
    public async Task<(AppUser User, Client Client, Invoice Invoice)> CreateUserWithIssuedInvoiceAsync(
        decimal invoiceAmount = 1000m,
        Role role = Role.Customer)
    {
        var (user, client) = await CreateUserWithClientAsync(role: role);
        var invoice = await CreateIssuedInvoiceAsync(client, amount: invoiceAmount);

        return (user, client, invoice);
    }

    /// <summary>
    /// Creates a PaymentAttempt securely locked in the database in a specific status.
    /// </summary>
    public async Task<PaymentAttempt> CreatePaymentAttemptAsync(
        Invoice invoice,
        decimal amount,
        PaymentProvider provider = PaymentProvider.Stripe,
        PaymentStatus status = PaymentStatus.Initializing,
        string? providerReference = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var attempt = PaymentAttempt.Reserve(invoice.Id, amount, provider, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(24));

        if (status != PaymentStatus.Initializing)
        {
            attempt.SetCheckoutDetails(
                providerReference ?? $"cs_test_{Guid.NewGuid().ToString("N")[..8]}",
                "https://checkout.stripe.com/pay");

            if (status == PaymentStatus.Succeeded)
                attempt.MarkAsSucceeded();
            else if (status == PaymentStatus.Failed)
                attempt.MarkAsFailed("Testing decline");
            else if (status == PaymentStatus.Expired)
                attempt.MarkAsExpired();
        }

        db.PaymentAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return attempt;
    }

    /// <summary>
    /// Creates a full accounting ledger Payment record.
    /// </summary>
    public async Task<Payment> CreateManualPaymentAsync(
        Invoice invoice,
        decimal amount,
        Guid receivedByUserId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        // Domain rule: Must apply payment to invoice first to sync balances correctly
        var trackedInvoice = await db.Invoices.FindAsync(invoice.Id);
        trackedInvoice!.ApplyPayment(amount);

        var payment = Payment.CreateManualPayment(
            invoice.Id,
            invoice.ClientId,
            amount,
            PaymentMethod.BankTransfer,
            DateTimeOffset.UtcNow,
            receivedByUserId,
            "Integration Test Payment",
            DateTimeOffset.UtcNow);

        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        return payment;
    }
}
