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
        // To Call the same api for 1000 times with one command
        // for /l %i in (1,1,10000) do curl -X POST "http://localhost:5000/api/orders" -H "accept: */*" -H "Content-Type: application/json" -d "{\"total\": 0}"
        Console.Clear();
        var orderId = Guid.CreateVersion7();
        var total = request.Total;
        await eventBus.PublishAsync(new OrderCreatedEvent(orderId, total));
        return Accepted(new { orderId, total });
    }

    [HttpPost("{times:long}")]
    public async Task<IActionResult> CreateOrder([FromRoute] long times, [FromBody] CreateOrderRequest request)
    {
        Console.Clear();

        times = times == 0 ? 1 : times;
        for (var i = 0; i < times; i++)
        {
            var orderId = Guid.CreateVersion7();
            var total = request.Total;
            await eventBus.PublishAsync(new OrderCreatedEvent(orderId, total));
        }
        return Accepted(new { orderId = Guid.CreateVersion7().ToString(), total = request.Total });
    }
}

public sealed record CreateOrderRequest(decimal Total);
