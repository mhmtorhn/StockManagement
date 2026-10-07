using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StockManagement.Entities
{
    public class SaleRefundPayment
    {
        public int Id { get; set; }

        public int SaleId { get; set; }

        public Sale Sale { get; set; } = null!;

        [Precision(18, 2)]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public DateTime RefundDate { get; set; }
            = DateTime.UtcNow;

        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public bool IsCancelled { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }
    }
}