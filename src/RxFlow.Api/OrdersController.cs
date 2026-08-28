using Microsoft.AspNetCore.Mvc;
using RxFlow.Application;
using RxFlow.Domain;

namespace RxFlow.Api;

[ApiController]
[Route("orders")]
[Microsoft.AspNetCore.Authorization.Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly SubmitOrderService _service;
    private readonly IOrderRepository _orders;

    public OrdersController(SubmitOrderService service, IOrderRepository orders) { _service = service; _orders = orders; }

    [HttpPost]
    public async Task<ActionResult<SubmitOrderResult>> Post(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest();
        try
        {
            var command = new SubmitOrderCommand(
                new Prescription(request.Sphere, request.Cylinder, request.Axis),
                new Frame(request.FrameId, request.FrameA, request.FrameB));
            var result = await _service.SubmitAsync(command, cancellationToken).ConfigureAwait(false);
            return Accepted($"/orders/{result.OrderId}", result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> Get(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return order is null ? NotFound() : Ok(new { order.Id, order.Status, FrameId = order.Frame.Id });
    }
}

public sealed record CreateOrderRequest(
    decimal Sphere,
    decimal Cylinder,
    int Axis,
    string FrameId,
    decimal FrameA,
    decimal FrameB);
