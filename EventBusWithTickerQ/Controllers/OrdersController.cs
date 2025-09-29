using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.Events;
using EventBusWithTickerQ.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EventBusWithTickerQ.Controllers;

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
        var orderId = Guid.NewGuid();
        var total = request.Total;
        await _eventBus.PublishAsync(new OrderCreatedEvent(orderId, total));
        return Accepted(new { orderId, total });
    }
}

public sealed record CreateOrderRequest(decimal Total);
