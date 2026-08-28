using Microsoft.AspNetCore.Mvc;
using RxFlow.Application;
using RxFlow.Domain;

namespace RxFlow.Api;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly SubmitOrderService _service;

    public OrdersController(SubmitOrderService service) => _service = service;

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
}

public sealed record CreateOrderRequest(
    decimal Sphere,
    decimal Cylinder,
    int Axis,
    string FrameId,
    decimal FrameA,
    decimal FrameB);
