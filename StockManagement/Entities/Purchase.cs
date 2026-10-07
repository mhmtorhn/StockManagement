namespace StockManagement.Entities
{
    public class Purchase
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }

        public Supplier Supplier { get; set; } = null!;

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PaymentStatus PaymentStatus { get; set; }
            = PaymentStatus.Unpaid;

        public PurchaseStatus Status { get; set; }
            = PurchaseStatus.Draft;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancellationReason { get; set; }

        public ICollection<PurchaseItem> PurchaseItems { get; set; }
            = new List<PurchaseItem>();

        public ICollection<PurchasePayment> PurchasePayments { get; set; }
            = new List<PurchasePayment>();
    }
}