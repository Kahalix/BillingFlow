// File: tests/BillingFlow.Application.Tests/Helpers/DomainTestFactory.cs
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
}
