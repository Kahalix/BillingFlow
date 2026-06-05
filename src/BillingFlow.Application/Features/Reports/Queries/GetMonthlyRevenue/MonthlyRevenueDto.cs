using System;

namespace BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;

public record MonthlyRevenueDto(
    Guid ClientId,
    string CompanyName,
    string TaxId,
    int TotalInvoices,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal OutstandingDebt
);
