namespace OutboxPatternWithHangfire.Events
{
    public interface IOutboxEvent { }

    public class OrderCreatedEvent : IOutboxEvent
    {
        public Guid OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderShippedEvent : IOutboxEvent
    {
        public Guid OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime ShippedAt { get; set; }
    }

    public class MailSendEvent : IOutboxEvent
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class InventoryUpdateEvent : IOutboxEvent
    {
        public Guid OrderId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
