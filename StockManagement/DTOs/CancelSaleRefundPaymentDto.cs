using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class CancelSaleRefundPaymentDto
    {
        [Required(ErrorMessage = "Para iadesi iptal nedeni zorunludur.")]
        [MaxLength(
            500,
            ErrorMessage = "İptal nedeni en fazla 500 karakter olabilir.")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}