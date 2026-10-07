using System.ComponentModel.DataAnnotations;
using StockManagement.Entities;

namespace StockManagement.DTOs
{
    public class UpdatePurchaseDto
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Geçerli bir tedarikçi seçilmelidir.")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Fatura numarası zorunludur.")]
        [MaxLength(
            100,
            ErrorMessage = "Fatura numarası en fazla 100 karakter olabilir.")]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime? PurchaseDate { get; set; }

        public DateTime? DueDate { get; set; }

        [EnumDataType(
            typeof(PaymentMethod),
            ErrorMessage = "Geçersiz ödeme yöntemi.")]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(
            500,
            ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Satın alma ürünleri zorunludur.")]
        [MinLength(
            1,
            ErrorMessage = "Satın almada en az bir ürün olmalıdır.")]
        public List<CreatePurchaseItemDto> Items { get; set; }
            = new List<CreatePurchaseItemDto>();
    }
}