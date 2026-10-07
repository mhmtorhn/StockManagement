using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class CreateSaleItemDto
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Geçerli bir ürün seçilmelidir.")]
        public int ProductId { get; set; }

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Satış miktarı en az 1 olmalıdır.")]
        public int Quantity { get; set; }

        [Range(
            0.01,
            double.MaxValue,
            ErrorMessage = "Birim fiyat 0'dan büyük olmalıdır.")]
        public decimal UnitPrice { get; set; }
    }
}