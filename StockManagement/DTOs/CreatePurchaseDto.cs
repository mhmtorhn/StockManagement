using System.ComponentModel.DataAnnotations;
using StockManagement.Entities;

namespace StockManagement.DTOs
{
    public class CreatePurchaseDto
    {
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        [Required]
        [MaxLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime? PurchaseDate { get; set; }

        public DateTime? DueDate { get; set; }

        [EnumDataType(typeof(PaymentMethod))]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<CreatePurchaseItemDto> Items { get; set; }
            = new List<CreatePurchaseItemDto>();
    }
}