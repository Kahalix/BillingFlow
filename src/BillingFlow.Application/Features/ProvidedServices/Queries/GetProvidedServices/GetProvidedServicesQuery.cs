using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Models;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServices;

public record GetProvidedServicesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? FilterByClientId = null,
    ProvidedServiceStatus? FilterByStatus = null,
    string? SearchTerm = null) : IRequest<PaginatedList<ProvidedServiceSummaryDto>>, IRequirePermission;
