using EventBusWithHangfire.Abstractions;
using EventBusWithHangfire.Events;
using EventBusWithHangfire.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EventBusWithHangfire.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IEventBus _eventBus;

    public OrdersController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        Console.Clear();
        // Simulate order creation
        var orderId = Guid.NewGuid();
        var total = request.Total;

        // Fire-and-forget publish using Hangfire
        await _eventBus.PublishAsync(new OrderCreatedEvent(orderId, total));

        return Accepted(new { orderId, total });
    }
}

public sealed record CreateOrderRequest(decimal Total);
