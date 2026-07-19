using System;
using System.Collections.Generic;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Invoices.Common.Models;

public record InvoiceItemModel(
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);
