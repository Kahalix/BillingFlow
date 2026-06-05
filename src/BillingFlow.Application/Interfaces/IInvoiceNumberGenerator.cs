using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Defines the contract for generating sequential invoice numbers.
/// Note: Depending on the underlying infrastructure (e.g., SQL Server SEQUENCE with CACHE), 
/// this does not guarantee strictly gapless numbering in the event of rollbacks, 
/// but ensures high concurrency and uniqueness.
/// </summary>
public interface IInvoiceNumberGenerator
{
    Task<string> GenerateNextNumberAsync(CancellationToken cancellationToken);
}
