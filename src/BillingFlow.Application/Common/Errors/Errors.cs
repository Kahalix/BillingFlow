using System;
using BillingFlow.Application.Common.Results;

namespace BillingFlow.Application.Common.Errors;

/// <summary>
/// Centralized directory of all expected business errors in the application.
/// Prevents magic strings and ensures consistency across command and query handlers.
/// </summary>
public static class Errors
{
    public static class Auth
    {
        public static readonly Error Unauthorized = new("Auth.Unauthorized", "Authentication is required to access this resource.", ErrorType.Unauthorized);
        public static readonly Error Forbidden = new("Auth.Forbidden", "Policy requirements not met for this resource.", ErrorType.Forbidden);
        public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Invalid email or password.", ErrorType.Unauthorized);
        public static readonly Error InvalidSession = new("Auth.InvalidSession", "Invalid session context.", ErrorType.Unauthorized);
        public static readonly Error InvalidRefreshToken = new("Auth.InvalidRefreshToken", "Invalid refresh token.", ErrorType.Unauthorized);
        public static readonly Error SessionCompromised = new("Auth.SessionCompromised", "Session compromise detected. The session has been revoked.", ErrorType.Unauthorized);
        public static readonly Error SessionExpired = new("Auth.SessionExpired", "This session has expired or been revoked.", ErrorType.Unauthorized);
        public static readonly Error ConcurrentModification = new("Auth.ConcurrentModification", "Concurrent session modification detected. Please log in again.", ErrorType.Conflict);
    }

    public static class Users
    {
        public static Error NotFound(Guid id) => new("User.NotFound", $"User with ID {id} was not found.", ErrorType.NotFound);
        public static readonly Error InvalidPassword = new("User.InvalidPassword", "The current password provided is incorrect.", ErrorType.Unauthorized);
        public static readonly Error EmailInUse = new("User.EmailInUse", "This email address is already associated with another account.", ErrorType.Conflict);
        public static readonly Error Suspended = new("User.Suspended", "This account is currently suspended.", ErrorType.Forbidden);
        public static readonly Error AdminRequired = new("User.AdminRequired", "Access denied. Administrator privileges required.", ErrorType.Forbidden);
    }

    public static class Tokens
    {
        public static readonly Error InvalidOrExpired = new("Token.InvalidOrExpired", "The token is invalid or has expired.", ErrorType.UnprocessableEntity);
        public static readonly Error ClaimedByOtherUser = new("Token.ClaimedByOtherUser", "This email address was registered by another user in the meantime.", ErrorType.Conflict);
    }

    public static class Clients
    {
        public static Error NotFound(Guid id) => new("Client.NotFound", $"Client profile with ID {id} could not be found.", ErrorType.NotFound);
        public static readonly Error ProfileNotFound = new("Client.ProfileNotFound", "You do not have an associated client profile.", ErrorType.NotFound);
        public static readonly Error UserAlreadyLinked = new("Client.UserAlreadyLinked", "This user is already linked to another client profile.", ErrorType.Conflict);
    }

    public static class ProvidedServices
    {
        public static Error NotFound(Guid id) => new("ProvidedService.NotFound", $"Provided service with ID {id} could not be found.", ErrorType.NotFound);
        public static readonly Error ClientNotActive = new("ProvidedService.ClientNotActive", "Cannot add billable services to a suspended or archived client.", ErrorType.Forbidden);
    }

    public static class Invoices
    {
        public static Error NotFound(Guid id) => new("Invoice.NotFound", $"Invoice with ID {id} could not be found.", ErrorType.NotFound);
        public static readonly Error ClientNotActive = new( "Invoice.ClientNotActive", "Cannot generate invoices for suspended or archived billing profiles.", ErrorType.Forbidden);
        public static readonly Error NoUnbilledServices = new("Invoice.NoUnbilledServices", "No unbilled provided services found for the specified period.", ErrorType.Conflict);
    }

    public static class Payments
    {
        public static Error NotFound(Guid id) => new("Payment.NotFound", $"Payment with ID {id} could not be found.", ErrorType.NotFound);

        public static Error InvoiceNotFound(Guid invoiceId) => new("Payment.InvoiceNotFound", $"Invoice with ID {invoiceId} could not be found.", ErrorType.NotFound);

        public static readonly Error InvoiceFullyPaid = new("Payment.InvoiceFullyPaid", "This invoice has already been fully paid.", ErrorType.Forbidden);

        public static readonly Error InvoiceNotActive = new("Payment.InvoiceNotActive", "Payment sessions can only be created for active, unpaid invoices.", ErrorType.Forbidden);

        public static readonly Error InvalidAmount = new("Payment.InvalidAmount", "Amount due must be strictly greater than zero to initiate a payment.", ErrorType.UnprocessableEntity);

        public static readonly Error SessionRaceCondition = new("Payment.SessionRaceCondition", "A payment session is already being created for this invoice. Please wait a moment and try again.", ErrorType.Conflict);
    }
}
