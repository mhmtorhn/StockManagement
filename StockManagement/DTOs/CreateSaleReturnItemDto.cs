using System.ComponentModel.DataAnnotations;

namespace StockManagement.DTOs
{
    public class CreateSaleReturnItemDto
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Geçerli bir satış kalemi seçilmelidir.")]
        public int SaleItemId { get; set; }

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "İade miktarı en az 1 olmalıdır.")]
        public int Quantity { get; set; }
    }
}