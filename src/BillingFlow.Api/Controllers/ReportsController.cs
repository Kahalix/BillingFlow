using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Executes a highly optimized Stored Procedure to generate a monthly revenue report.
    /// Restricted to executive and accounting roles.
    /// </summary>
    [HttpGet("monthly-revenue")]
    [Authorize(Policy = AppPermissions.ReportsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<MonthlyRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMonthlyRevenue(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        var query = new GetMonthlyRevenueQuery(year, month);
        var result = await sender.Send(query, cancellationToken);

        return Ok(result);
    }
}
