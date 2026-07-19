using System;
using System.Collections.Generic;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Invoices.Common.Models;

public record InvoiceClientModel(
    Guid ClientId,
    string CompanyName,
    string TaxId
);
