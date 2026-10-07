using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class CreateSaleReturnDto
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Geçerli bir satış seçilmelidir.")]
        public int SaleId { get; set; }

        [Required(ErrorMessage = "İade numarası zorunludur.")]
        [MaxLength(
            100,
            ErrorMessage = "İade numarası en fazla 100 karakter olabilir.")]
        public string ReturnNumber { get; set; } = string.Empty;

        public DateTime? ReturnDate { get; set; }

        [Required(ErrorMessage = "İade nedeni zorunludur.")]
        [MaxLength(
            500,
            ErrorMessage = "İade nedeni en fazla 500 karakter olabilir.")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "İade ürünleri zorunludur.")]
        [MinLength(
            1,
            ErrorMessage = "İadede en az bir ürün olmalıdır.")]
        public List<CreateSaleReturnItemDto> Items { get; set; }
            = new List<CreateSaleReturnItemDto>();
    }
}