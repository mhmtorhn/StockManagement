using System.ComponentModel.DataAnnotations;
using StockManagement.Entities;

namespace StockManagement.DTOs
{
    public class CreateCustomerDto
    {
        [EnumDataType(
            typeof(CustomerType),
            ErrorMessage = "Geçersiz müşteri türü.")]
        public CustomerType CustomerType { get; set; }

        [Required(ErrorMessage = "Müşteri adı zorunludur.")]
        [MaxLength(
            150,
            ErrorMessage = "Müşteri adı en fazla 150 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(
            150,
            ErrorMessage = "Yetkili kişi en fazla 150 karakter olabilir.")]
        public string ContactPerson { get; set; } = string.Empty;

        [MaxLength(
            30,
            ErrorMessage = "Telefon numarası en fazla 30 karakter olabilir.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(
            150,
            ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(
            500,
            ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string Address { get; set; } = string.Empty;

        [MaxLength(
            50,
            ErrorMessage = "Vergi numarası en fazla 50 karakter olabilir.")]
        public string TaxNumber { get; set; } = string.Empty;

        [MaxLength(
            500,
            ErrorMessage = "Notlar en fazla 500 karakter olabilir.")]
        public string Notes { get; set; } = string.Empty;
    }
}