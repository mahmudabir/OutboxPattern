using EventBusWithQuartz.Events;
using EventBusWithQuartz.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EventBusWithQuartz.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IEventBus eventBus) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        Console.Clear();
        var orderId = Guid.NewGuid();
        var total = request.Total;
        await eventBus.PublishAsync(new OrderCreatedEvent(orderId, total));
        return Accepted(new { orderId, total });
    }
}

public sealed record CreateOrderRequest(decimal Total);
