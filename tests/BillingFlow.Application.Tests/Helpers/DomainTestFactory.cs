using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.ValueObjects;

namespace BillingFlow.Application.Tests.Helpers;

/// <summary>
/// A centralized Test Data Builder for Domain Entities.
/// Exposes semantic, domain-aligned creation methods to guarantee aggregate consistency.
/// Reflection is strictly limited to ID generation for in-memory databases/mocks.
/// </summary>
public static class DomainTestFactory
{
    public static void SetPrivateId<T>(T entity, Guid id) where T : Entity
    {
        var property = typeof(T).GetProperty("Id");
        if (property != null && property.CanWrite)
        {
            property.SetValue(entity, id);
        }
    }

    public static AppUser CreateActiveUser(string email = "test@domain.com", Role role = Role.Customer, Guid? id = null)
    {
        var user = new AppUser(email, "mocked_hash", role, DateTimeOffset.UtcNow);
        SetPrivateId(user, id ?? Guid.NewGuid());
        return user;
    }

    /// <summary>
    /// Creates a mock UserToken. 
    /// Optionally marks it as consumed to simulate Replay Attack scenarios.
    /// </summary>
    public static UserToken CreateUserToken(
        Guid userId,
        Guid? sessionId = null,
        UserTokenType type = UserTokenType.RefreshToken,
        bool isConsumed = false,
        string hash = "mocked_token_hash",
        Guid? id = null)
    {
        var token = new UserToken(
            userId,
            sessionId ?? Guid.NewGuid(),
            type,
            hash,
            DateTimeOffset.UtcNow.AddDays(7), // Default active expiry
            DateTimeOffset.UtcNow,
            null);

        if (isConsumed)
        {
            token.MarkAsConsumed(TimeProvider.System);
        }

        SetPrivateId(token, id ?? Guid.NewGuid());
        return token;
    }
    public static Client CreateActiveClient(Guid? id = null)
    {
        var client = Client.Create(Guid.NewGuid(), "Test Corp", "TAX-000", new Address("St", "City", "Zip", "USA"));
        SetPrivateId(client, id ?? Guid.NewGuid());
        return client;
    }

    /// <summary>
    /// Creates an archived client using the strict Domain Path.
    /// This ensures Domain Events are fired and UserIds are properly unlinked.
    /// </summary>
    public static Client CreateArchivedClient(Guid? id = null)
    {
        var client = CreateActiveClient(id);
        client.Archive(); // LEGAL DDD PATH!
        return client;
    }

    public static Invoice CreateDraftInvoice(Guid? id = null, Guid? clientId = null)
    {
        var invoice = Invoice.Create(clientId ?? Guid.NewGuid(), Guid.NewGuid(), "INV-TEST", DateTimeOffset.UtcNow);
        SetPrivateId(invoice, id ?? Guid.NewGuid());
        return invoice;
    }

    /// <summary>
    /// Transitions a Draft invoice to Unpaid using legal Domain methods.
    /// Guarantees aggregate consistency (requires a line item before issuing).
    /// </summary>
    public static Invoice CreateUnpaidInvoice(Guid? id = null, Guid? clientId = null)
    {
        var invoice = CreateDraftInvoice(id, clientId);
        invoice.AddLineItem("Mock Service", 100m, 1);
        invoice.Issue(); // LEGAL DDD PATH!
        return invoice;
    }

    /// <summary>
    /// Bypasses domain logic to instantiate a raw InvoiceItem.
    /// Used strictly for mocking EF Core Data Providers in Unit Tests.
    /// </summary>
    public static InvoiceItem CreateMockInvoiceItem(Guid invoiceId)
    {
        var item = (InvoiceItem)Activator.CreateInstance(typeof(InvoiceItem), true)!;
        typeof(InvoiceItem).GetProperty("InvoiceId")!.SetValue(item, invoiceId);
        typeof(InvoiceItem).GetProperty("Description")!.SetValue(item, "Consulting");
        typeof(InvoiceItem).GetProperty("UnitPrice")!.SetValue(item, 1000m);
        typeof(InvoiceItem).GetProperty("Quantity")!.SetValue(item, 2);
        typeof(InvoiceItem).GetProperty("LineTotal")!.SetValue(item, 2000m);
        return item;
    }

    /// <summary>
    /// Creates a PaymentAttempt in the 'Started' state (Phase 2), simulating a successful Stripe API initialization.
    /// </summary>
    public static PaymentAttempt CreateStartedPaymentAttempt(
        Guid invoiceId,
        decimal amount,
        string providerReference = "cs_test_123",
        Guid? id = null)
    {
        // 1. Phase 1: Reservation
        var attempt = PaymentAttempt.Reserve(
            invoiceId, amount, PaymentProvider.Stripe, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        // 2. Phase 2: Started
        attempt.SetCheckoutDetails(providerReference, $"https://checkout.stripe.com/pay/{providerReference}");

        SetPrivateId(attempt, id ?? Guid.NewGuid());
        return attempt;
    }

    /// <summary>
    /// Creates an offline manual payment ledger record.
    /// </summary>
    public static Payment CreateManualPayment(Guid invoiceId, decimal amount, Guid? id = null, Guid? clientId = null)
    {
        var payment = Payment.CreateManualPayment(
            invoiceId,
            clientId ?? Guid.NewGuid(),
            amount,
            PaymentMethod.BankTransfer,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Test payment",
            DateTimeOffset.UtcNow);

        SetPrivateId(payment, id ?? Guid.NewGuid());
        return payment;
    }

    public static ProvidedService CreateUnbilledProvidedService(
        Guid? clientId = null,
        decimal amount = 100m,
        Guid? id = null)
    {
        var service = ProvidedService.Create(
            clientId ?? Guid.NewGuid(),
            "Mocked Service",
            amount,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        SetPrivateId(service, id ?? Guid.NewGuid());
        return service;
    }

    public static ProvidedService CreateBilledProvidedService(
        Guid? clientId = null,
        Guid? invoiceId = null,
        Guid? id = null)
    {
        var service = CreateUnbilledProvidedService(clientId, 100m, id);

        // Legal DDD path to transition the state machine to Billed
        service.MarkAsBilled(invoiceId ?? Guid.NewGuid(), DateTimeOffset.UtcNow);
        return service;
    }
}
