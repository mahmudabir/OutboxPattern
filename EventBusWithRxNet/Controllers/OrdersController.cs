using Microsoft.AspNetCore.Mvc;
using EventBusWithRxNet.Infrastructure;
using EventBusWithRxNet.Events;

namespace EventBusWithRxNet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        public OrdersController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        [HttpPost("place")]
        public IActionResult PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            // ... business logic for placing order ...
            var orderId = Guid.NewGuid().ToString();
            _eventBus.Publish(new OrderPlacedEvent { OrderId = orderId, UserId = request.UserId });
            return Accepted(new { OrderId = orderId });
        }

        [HttpPost("pay")]
        public IActionResult PayOrder([FromBody] PayOrderRequest request)
        {
            // ... business logic for paying order ...
            _eventBus.Publish(new OrderPaidEvent { OrderId = request.OrderId, PaidAmount = request.Amount });
            return Accepted();
        }
    }

    public class PlaceOrderRequest
    {
        public string UserId { get; set; }
    }
    public class PayOrderRequest
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}