namespace StockManagement.Entities
{
    public class SaleReturn
    {
        public int Id { get; set; }

        public int SaleId { get; set; }

        public Sale Sale { get; set; } = null!;

        public string ReturnNumber { get; set; } = string.Empty;

        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        public decimal TotalRefundAmount { get; set; }

        public decimal TotalCostAmount { get; set; }

        public decimal TotalProfitAdjustment { get; set; }

        public string Reason { get; set; } = string.Empty;

        public SaleReturnStatus Status { get; set; }
            = SaleReturnStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancellationReason { get; set; }

        public ICollection<SaleReturnItem> SaleReturnItems { get; set; }
            = new List<SaleReturnItem>();
    }
}