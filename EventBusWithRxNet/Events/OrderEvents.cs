namespace EventBusWithRxNet.Events
{
    public class OrderPlacedEvent
    {
        public string OrderId { get; set; }
        public string UserId { get; set; }
    }
    public class OrderPaidEvent
    {
        public string OrderId { get; set; }
        public decimal PaidAmount { get; set; }
    }
}