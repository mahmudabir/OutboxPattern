using System;
using System.ComponentModel.DataAnnotations;

namespace OutboxPatternWithHangfire.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // New fields for shipping
        public bool Shipped { get; set; } = false;
        public DateTime? ShippedAt { get; set; }
    }
}