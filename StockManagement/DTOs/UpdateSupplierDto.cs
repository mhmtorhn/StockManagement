namespace StockManagement.DTOs
{
    public class UpdateSupplierDto
    {
        public string Name { get; set; } = string.Empty;

        public string? ContactPerson { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }
    }
}