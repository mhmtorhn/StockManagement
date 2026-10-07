using System.ComponentModel.DataAnnotations;
using StockManagement.Entities;

namespace StockManagement.DTOs
{
    public class CreateSalePaymentDto
    {
        [Range(
            0.01,
            double.MaxValue,
            ErrorMessage = "Tahsilat tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        [EnumDataType(
            typeof(PaymentMethod),
            ErrorMessage = "Geçersiz ödeme yöntemi.")]
        public PaymentMethod PaymentMethod { get; set; }

        public DateTime? PaymentDate { get; set; }

        [MaxLength(
            100,
            ErrorMessage = "Referans numarası en fazla 100 karakter olabilir.")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [MaxLength(
            500,
            ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; } = string.Empty;
    }
}