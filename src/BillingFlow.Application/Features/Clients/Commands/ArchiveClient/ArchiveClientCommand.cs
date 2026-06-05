using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.ArchiveClient;

/// <summary>
/// Command to soft-delete (archive) a client profile.
/// </summary>
public record ArchiveClientCommand(Guid ClientId) : IRequest, IRequirePermission;
