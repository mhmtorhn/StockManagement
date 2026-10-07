using System.ComponentModel.DataAnnotations;

namespace StockManagement.Entities
{
    public class SalePayment
    {
        public int Id { get; set; }

        public int SaleId { get; set; }

        public Sale Sale { get; set; } = null!;

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string ReferenceNumber { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsCancelled { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }
    }
}