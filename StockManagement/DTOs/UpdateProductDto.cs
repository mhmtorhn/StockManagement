using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class UpdateProductDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal PurchasePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SalePrice { get; set; }

        [Range(0, int.MaxValue)]
        public int MinimumStockLevel { get; set; }

        [Range(1, int.MaxValue)]
        public int? CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int? SupplierId { get; set; }
    }
}