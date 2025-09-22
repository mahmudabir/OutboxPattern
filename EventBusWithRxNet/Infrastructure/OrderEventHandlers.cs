using System;
using System.Threading;
using System.Threading.Tasks;
using EventBusWithRxNet.Events;

namespace EventBusWithRxNet.Infrastructure
{
    public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
    {
        public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Handler] OrderPlacedEvent: OrderId={@event.OrderId}, UserId={@event.UserId}");
            return Task.CompletedTask;
        }
    }

    public class EmailAfterOrderPlacedHandler : IEventHandler<OrderPlacedEvent>
    {
        public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Handler] EmailAfterOrderPlacedHandler: OrderId={@event.OrderId}, UserId={@event.UserId}");
            return Task.CompletedTask;
        }
    }

    public class OrderPaidHandler : IEventHandler<OrderPaidEvent>
    {
        public Task HandleAsync(OrderPaidEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[Handler] OrderPaidEvent: OrderId={@event.OrderId}, PaidAmount={@event.PaidAmount}");
            return Task.CompletedTask;
        }
    }
}