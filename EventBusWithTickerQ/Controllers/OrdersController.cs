using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.Events;
using EventBusWithTickerQ.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EventBusWithTickerQ.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IEventBus eventBus) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        Console.Clear();
        var orderId = Guid.CreateVersion7();
        var total = request.Total;
        await eventBus.PublishAsync(new OrderCreateEvent(orderId, total));
        return Accepted(new { orderId, total });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateOrder([FromBody] CreateOrderRequest request)
    {
        Console.Clear();
        var orderId = Guid.CreateVersion7();
        var total = request.Total;
        await eventBus.PublishAsync(new OrderUpdateEvent(orderId, total));
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
            await eventBus.PublishAsync(new OrderCreateEvent(orderId, total));
        }
        return Accepted(new { orderId = Guid.CreateVersion7().ToString(), total = request.Total });
    }
}

public sealed record CreateOrderRequest(decimal Total);
