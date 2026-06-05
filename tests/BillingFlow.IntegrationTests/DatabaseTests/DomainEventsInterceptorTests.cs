using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Infrastructure.Database;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using MediatR;

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Xunit;

namespace BillingFlow.IntegrationTests.DatabaseTests;

public class DomainEventsInterceptorTests : BaseIntegrationTest
{
    public DomainEventsInterceptorTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityHasDomainEvents_ShouldDispatchViaMediatRAndClearQueue()
    {
        // 1. Arrange
        var mockHandler = new Mock<INotificationHandler<PaymentRecordedEvent>>();

        var factoryWithSpy = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<INotificationHandler<PaymentRecordedEvent>>(_ => mockHandler.Object);
            });
        });

        // Use the centralized factory to correctly orchestrate all foreign keys and saves
        var (user, client, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync();

        using var scope = factoryWithSpy.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        db.Users.Attach(user);
        db.Clients.Attach(client);
        db.Invoices.Attach(invoice);

        var payment = Payment.CreateManualPayment(
            invoice.Id,
            client.Id,
            500m,
            PaymentMethod.BankTransfer,
            DateTimeOffset.UtcNow,
            user.Id,
            "Interceptor test",
            DateTimeOffset.UtcNow);

        // Pre-save assertion: The event must be queued inside the entity
        payment.DomainEvents.Should().HaveCount(1);

        db.Payments.Add(payment);

        // 2. Act
        // This triggers the DispatchDomainEventsInterceptor under the hood
        await db.SaveChangesAsync();

        // 3. Assert
        // Verify that the Interceptor successfully routed the Domain Event through MediatR pipeline
        mockHandler.Verify(h => h.Handle(
            It.Is<PaymentRecordedEvent>(e => e.PaymentId == payment.Id && e.Amount == 500m),
            It.IsAny<CancellationToken>()),
            Times.Once,
            "The EF Core Interceptor failed to dispatch the Domain Event to MediatR.");

        // Verify that the Interceptor safely cleared the events after publishing
        payment.DomainEvents.Should().BeEmpty();
    }
}
