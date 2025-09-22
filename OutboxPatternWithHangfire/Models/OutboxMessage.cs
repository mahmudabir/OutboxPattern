using System;
using System.ComponentModel.DataAnnotations;

namespace OutboxPatternWithHangfire.Models
{
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public bool Processed { get; set; } = false;
        public DateTime? ProcessedOn { get; set; }
    }
}