namespace StockManagement.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public CustomerType CustomerType { get; set; }
            = CustomerType.Individual;

        public string Name { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string TaxNumber { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Sale> Sales { get; set; }
            = new List<Sale>();
    }
}