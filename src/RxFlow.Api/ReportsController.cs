using Microsoft.AspNetCore.Mvc;
using RxFlow.Reporting;

namespace RxFlow.Api;

/// <summary>
/// The reporting/status-view HTTP surface. Read-only: it never accepts a write, and it returns the
/// <see cref="OrderStatusView"/> projection rather than the write-model order entity.
/// </summary>
[ApiController]
[Route("reports/orders")]
[Microsoft.AspNetCore.Authorization.Authorize]
public sealed class ReportsController(OrderReportingService reporting) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderStatusView>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var view = await reporting.GetOrderAsync(id, cancellationToken).ConfigureAwait(false);
        return view is null ? NotFound() : Ok(view);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderStatusView>>> ListOrders([FromQuery] int take, CancellationToken cancellationToken)
    {
        if (take == 0) take = 20;
        try
        {
            return Ok(await reporting.ListOrdersAsync(take, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
