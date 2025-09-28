using System.ComponentModel.DataAnnotations;

namespace OutboxPatternWithQuartz.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Shipped { get; set; } = false;
    public DateTime? ShippedAt { get; set; }
}
