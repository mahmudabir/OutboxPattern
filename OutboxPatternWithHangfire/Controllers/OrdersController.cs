using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxPatternWithHangfire.Data;
using OutboxPatternWithHangfire.Models;
using OutboxPatternWithHangfire.Events;
using System.Text.Json;

namespace OutboxPatternWithHangfire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public OrdersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            order.Id = Guid.CreateVersion7();
            order.CreatedAt = DateTime.UtcNow;
            order.Shipped = false;
            order.ShippedAt = null;
            _db.Orders.Add(order);
            // Add outbox messages
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt
            };
            var mailSendEvent = new MailSendEvent
            {
                To = order.CustomerName,
                Subject = "Order Confirmation",
                Body = $"Your order {order.Id} has been created."
            };
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "OrderCreated",
                Content = JsonSerializer.Serialize(orderCreatedEvent),
                OccurredOn = DateTime.UtcNow
            });
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = "MailSend",
                Content = JsonSerializer.Serialize(mailSendEvent),
                OccurredOn = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        // New endpoint to ship an order
        [HttpPost("{id}/ship")]
        public async Task<IActionResult> ShipOrder(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.Shipped) return BadRequest("Order already shipped.");
            order.Shipped = true;
            order.ShippedAt = DateTime.UtcNow;
            // Add outbox messages for shipping
            var orderShippedEvent = new OrderShippedEvent
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                ShippedAt = order.ShippedAt.Value
            };
            var mailSendEvent = new MailSendEvent
            {
                To = order.CustomerName,
                Subject = "Order Shipped",
                Body = $"Your order {order.Id} has been shipped."
            };
            var inventoryUpdateEvent = new InventoryUpdateEvent
            {
                OrderId = order.Id,
                Action = "DecrementStock"
            };
            _db.OutboxMessages.AddRange([
                new OutboxMessage
                {
                    Id = Guid.CreateVersion7(),
                    Type = "OrderShipped",
                    Content = JsonSerializer.Serialize(orderShippedEvent),
                    OccurredOn = DateTime.UtcNow
                },
                new OutboxMessage
                {
                    Id = Guid.CreateVersion7(),
                    Type = "MailSend",
                    Content = JsonSerializer.Serialize(mailSendEvent),
                    OccurredOn = DateTime.UtcNow
                },
                new OutboxMessage
                {
                    Id = Guid.CreateVersion7(),
                    Type = "InventoryUpdate",
                    Content = JsonSerializer.Serialize(inventoryUpdateEvent),
                    OccurredOn = DateTime.UtcNow
                }
            ]);
            await _db.SaveChangesAsync();
            return Ok(order);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _db.Orders.ToListAsync();
            return Ok(orders);
        }
    }
}