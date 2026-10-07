namespace StockManagement.Entities
{
    public class SaleReturnItem
    {
        public int Id { get; set; }

        public int SaleReturnId { get; set; }

        public SaleReturn SaleReturn { get; set; } = null!;

        public int SaleItemId { get; set; }

        public SaleItem SaleItem { get; set; } = null!;

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal CostPrice { get; set; }

        public decimal RefundAmount { get; set; }

        public decimal ProfitAdjustment { get; set; }
    }
}