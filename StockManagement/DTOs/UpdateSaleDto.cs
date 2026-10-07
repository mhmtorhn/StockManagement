using System.ComponentModel.DataAnnotations;
using StockManagement.Entities;

namespace StockManagement.DTOs
{
    public class UpdateSaleDto
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Geçerli bir müşteri seçilmelidir.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Fatura numarası zorunludur.")]
        [MaxLength(
            100,
            ErrorMessage = "Fatura numarası en fazla 100 karakter olabilir.")]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime? SaleDate { get; set; }

        public DateTime? DueDate { get; set; }

        [EnumDataType(
            typeof(PaymentMethod),
            ErrorMessage = "Geçersiz ödeme yöntemi.")]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(
            500,
            ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Satış ürünleri zorunludur.")]
        [MinLength(
            1,
            ErrorMessage = "Satışta en az bir ürün olmalıdır.")]
        public List<CreateSaleItemDto> Items { get; set; }
            = new List<CreateSaleItemDto>();
    }
}