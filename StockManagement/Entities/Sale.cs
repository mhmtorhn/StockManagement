using Microsoft.EntityFrameworkCore;

namespace StockManagement.Entities
{
    public class Sale
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        [Precision(18, 2)]
        public decimal ReturnedAmount { get; set; }

        [Precision(18, 2)]
        public decimal NetAmount { get; set; }

        [Precision(18, 2)]
        public decimal PaidAmount { get; set; }

        [Precision(18, 2)]
        public decimal RemainingAmount { get; set; }

        [Precision(18, 2)]
        public decimal CustomerRefundedAmount { get; set; }

        [Precision(18, 2)]
        public decimal RefundDueAmount { get; set; }

        [Precision(18, 2)]
        public decimal TotalProfit { get; set; }

        [Precision(18, 2)]
        public decimal ReturnedProfitAmount { get; set; }

        [Precision(18, 2)]
        public decimal NetProfit { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PaymentStatus PaymentStatus { get; set; }
            = PaymentStatus.Unpaid;

        public SaleStatus Status { get; set; }
            = SaleStatus.Draft;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancellationReason { get; set; }

        public ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();

        public ICollection<SalePayment> SalePayments { get; set; }
            = new List<SalePayment>();

        public ICollection<SaleReturn> SaleReturns { get; set; }
            = new List<SaleReturn>();

        public ICollection<SaleRefundPayment> SaleRefundPayments
        { get; set; } = new List<SaleRefundPayment>();
    }
}