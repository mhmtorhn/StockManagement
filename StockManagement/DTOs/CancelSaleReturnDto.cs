using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class CancelSaleReturnDto
    {
        [Required(ErrorMessage = "İade iptal nedeni zorunludur.")]
        [MaxLength(
            500,
            ErrorMessage = "İptal nedeni en fazla 500 karakter olabilir.")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}