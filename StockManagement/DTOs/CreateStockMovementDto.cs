using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class CreateStockMovementDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}