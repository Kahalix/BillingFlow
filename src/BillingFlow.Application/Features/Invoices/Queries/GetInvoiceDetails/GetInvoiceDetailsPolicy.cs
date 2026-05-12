//// File: src/BillingFlow.Application/Features/Invoices/Queries/GetInvoiceDetails/GetInvoiceDetailsPolicy.cs
//using BillingFlow.Application.Authorization.Requirements;
//using BillingFlow.Application.Interfaces;
//using BillingFlow.Domain.Enums;

//using Microsoft.EntityFrameworkCore;

//namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;

///// <summary>
///// Dynamic policy evaluating whether the current user is allowed to view the requested invoice.
///// </summary>
//public class GetInvoiceDetailsPolicy(
//    ICurrentUserService currentUserService,
//    IApplicationDbContext dbContext) : IAuthorizationPolicy<GetInvoiceDetailsQuery>
//{
//    public async Task<bool> CanExecuteAsync(GetInvoiceDetailsQuery request, CancellationToken cancellationToken)
//    {
//        // 1. Admins and Accountants have global access
//        if (currentUserService.UserRole == Role.Admin || currentUserService.UserRole == Role.Accountant)
//        {
//            return true;
//        }

//        // 2. Customers can only view their own invoices
//        if (currentUserService.UserRole == Role.Customer)
//        {
//            var client = await dbContext.Clients
//                .AsNoTracking()
//                .FirstOrDefaultAsync(c => c.UserId == currentUserService.UserId, cancellationToken);

//            if (client is null)
//                return false;

//            // Check data ownership directly in the database
//            bool isOwner = await dbContext.Invoices
//                .AnyAsync(i => i.Id == request.InvoiceId && i.ClientId == client.Id, cancellationToken);

//            return isOwner;
//        }

//        // Default to deny
//        return false;
//    }
//}
