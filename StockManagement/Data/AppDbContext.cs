using Microsoft.EntityFrameworkCore;
using StockManagement.Entities;

namespace StockManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<StockMovement> StockMovements { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<Purchase> Purchases { get; set; }

        public DbSet<PurchaseItem> PurchaseItems { get; set; }

        public DbSet<PurchasePayment> PurchasePayments { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Sale> Sales { get; set; }

        public DbSet<SaleItem> SaleItems { get; set; }

        public DbSet<SalePayment> SalePayments { get; set; }

        public DbSet<SaleReturn> SaleReturns { get; set; }

        public DbSet<SaleReturnItem> SaleReturnItems { get; set; }

        public DbSet<SaleRefundPayment> SaleRefundPayments
        { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(user => user.Id);

                entity.Property(user => user.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(user => user.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(user => user.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(user => user.Role)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(30);

                entity.Property(user => user.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(user => user.CreatedAt)
                    .IsRequired();

                entity.Property(user => user.LastLoginAt);

                entity.HasIndex(user => user.Username)
                    .IsUnique();

                entity.HasIndex(user => user.Email)
                    .IsUnique();
            });

            modelBuilder.Entity<StockMovement>()
                .HasOne(stockMovement => stockMovement.Product)
                .WithMany(product => product.StockMovements)
                .HasForeignKey(stockMovement =>
                    stockMovement.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(product => product.Supplier)
                .WithMany(supplier => supplier.Products)
                .HasForeignKey(product => product.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Purchase>()
                .HasOne(purchase => purchase.Supplier)
                .WithMany(supplier => supplier.Purchases)
                .HasForeignKey(purchase => purchase.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(purchaseItem => purchaseItem.Purchase)
                .WithMany(purchase => purchase.PurchaseItems)
                .HasForeignKey(purchaseItem =>
                    purchaseItem.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(purchaseItem => purchaseItem.Product)
                .WithMany(product => product.PurchaseItems)
                .HasForeignKey(purchaseItem =>
                    purchaseItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchasePayment>()
                .HasOne(purchasePayment =>
                    purchasePayment.Purchase)
                .WithMany(purchase =>
                    purchase.PurchasePayments)
                .HasForeignKey(purchasePayment =>
                    purchasePayment.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sale>()
                .HasOne(sale => sale.Customer)
                .WithMany(customer => customer.Sales)
                .HasForeignKey(sale => sale.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleItem>()
                .HasOne(saleItem => saleItem.Sale)
                .WithMany(sale => sale.SaleItems)
                .HasForeignKey(saleItem => saleItem.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleItem>()
                .HasOne(saleItem => saleItem.Product)
                .WithMany()
                .HasForeignKey(saleItem =>
                    saleItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalePayment>()
                .HasOne(salePayment => salePayment.Sale)
                .WithMany(sale => sale.SalePayments)
                .HasForeignKey(salePayment =>
                    salePayment.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleReturn>()
                .HasOne(saleReturn => saleReturn.Sale)
                .WithMany(sale => sale.SaleReturns)
                .HasForeignKey(saleReturn =>
                    saleReturn.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleReturnItem>()
                .HasOne(returnItem =>
                    returnItem.SaleReturn)
                .WithMany(saleReturn =>
                    saleReturn.SaleReturnItems)
                .HasForeignKey(returnItem =>
                    returnItem.SaleReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleReturnItem>()
                .HasOne(returnItem =>
                    returnItem.SaleItem)
                .WithMany()
                .HasForeignKey(returnItem =>
                    returnItem.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleReturnItem>()
                .HasOne(returnItem =>
                    returnItem.Product)
                .WithMany()
                .HasForeignKey(returnItem =>
                    returnItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleRefundPayment>()
                .HasOne(refundPayment =>
                    refundPayment.Sale)
                .WithMany(sale =>
                    sale.SaleRefundPayments)
                .HasForeignKey(refundPayment =>
                    refundPayment.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Purchase>()
                .Property(purchase => purchase.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Purchase>()
                .Property(purchase => purchase.PaidAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Purchase>()
                .Property(purchase =>
                    purchase.RemainingAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseItem>()
                .Property(purchaseItem =>
                    purchaseItem.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseItem>()
                .Property(purchaseItem =>
                    purchaseItem.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchasePayment>()
                .Property(purchasePayment =>
                    purchasePayment.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchasePayment>()
                .Property(purchasePayment =>
                    purchasePayment.ReferenceNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<PurchasePayment>()
                .Property(purchasePayment =>
                    purchasePayment.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Customer>()
                .Property(customer => customer.Name)
                .HasMaxLength(150);

            modelBuilder.Entity<Customer>()
                .Property(customer =>
                    customer.ContactPerson)
                .HasMaxLength(150);

            modelBuilder.Entity<Customer>()
                .Property(customer =>
                    customer.PhoneNumber)
                .HasMaxLength(30);

            modelBuilder.Entity<Customer>()
                .Property(customer => customer.Email)
                .HasMaxLength(150);

            modelBuilder.Entity<Customer>()
                .Property(customer => customer.Address)
                .HasMaxLength(500);

            modelBuilder.Entity<Customer>()
                .Property(customer =>
                    customer.TaxNumber)
                .HasMaxLength(50);

            modelBuilder.Entity<Customer>()
                .Property(customer => customer.Notes)
                .HasMaxLength(500);

            modelBuilder.Entity<Customer>()
                .HasIndex(customer => customer.TaxNumber)
                .IsUnique()
                .HasFilter("\"TaxNumber\" <> ''");

            modelBuilder.Entity<Customer>()
                .HasIndex(customer => customer.Name);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.InvoiceNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Sale>()
                .Property(sale =>
                    sale.CancellationReason)
                .HasMaxLength(500);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.ReturnedAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.NetAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.PaidAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale =>
                    sale.RemainingAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale =>
                    sale.CustomerRefundedAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale =>
                    sale.RefundDueAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.TotalProfit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale =>
                    sale.ReturnedProfitAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(sale => sale.NetProfit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .HasIndex(sale => sale.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<SaleItem>()
                .Property(saleItem => saleItem.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleItem>()
                .Property(saleItem => saleItem.CostPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleItem>()
                .Property(saleItem => saleItem.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleItem>()
                .Property(saleItem =>
                    saleItem.ProfitAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SalePayment>()
                .Property(salePayment => salePayment.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SalePayment>()
                .Property(salePayment =>
                    salePayment.ReferenceNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<SalePayment>()
                .Property(salePayment =>
                    salePayment.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<SalePayment>()
                .Property(salePayment =>
                    salePayment.CancellationReason)
                .HasMaxLength(500);

            modelBuilder.Entity<SaleReturn>()
                .Property(saleReturn =>
                    saleReturn.ReturnNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<SaleReturn>()
                .Property(saleReturn => saleReturn.Reason)
                .HasMaxLength(500);

            modelBuilder.Entity<SaleReturn>()
                .Property(saleReturn =>
                    saleReturn.CancellationReason)
                .HasMaxLength(500);

            modelBuilder.Entity<SaleReturn>()
                .Property(saleReturn =>
                    saleReturn.TotalRefundAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleReturn>()
                .Property(saleReturn =>
                    saleReturn.TotalCostAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleReturn>()
                .Property(saleReturn =>
                    saleReturn.TotalProfitAdjustment)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleReturn>()
                .HasIndex(saleReturn =>
                    saleReturn.ReturnNumber)
                .IsUnique();

            modelBuilder.Entity<SaleReturnItem>()
                .Property(returnItem =>
                    returnItem.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleReturnItem>()
                .Property(returnItem =>
                    returnItem.CostPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleReturnItem>()
                .Property(returnItem =>
                    returnItem.RefundAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleReturnItem>()
                .Property(returnItem =>
                    returnItem.ProfitAdjustment)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleRefundPayment>()
                .Property(refundPayment =>
                    refundPayment.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleRefundPayment>()
                .Property(refundPayment =>
                    refundPayment.ReferenceNumber)
                .HasMaxLength(100);

            modelBuilder.Entity<SaleRefundPayment>()
                .Property(refundPayment =>
                    refundPayment.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<SaleRefundPayment>()
                .Property(refundPayment =>
                    refundPayment.CancellationReason)
                .HasMaxLength(500);
        }

        public override int SaveChanges()
        {
            UpdateSaleFinancialTotals();

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            UpdateSaleFinancialTotals();

            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateSaleFinancialTotals()
        {
            var changedSales = ChangeTracker
                .Entries<Sale>()
                .Where(entry =>
                    entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified)
                .Select(entry => entry.Entity)
                .ToList();

            foreach (var sale in changedSales)
            {
                sale.ReturnedAmount = Math.Max(
                    0,
                    sale.ReturnedAmount);

                sale.CustomerRefundedAmount = Math.Max(
                    0,
                    sale.CustomerRefundedAmount);

                sale.ReturnedProfitAmount = Math.Max(
                    0,
                    sale.ReturnedProfitAmount);

                sale.NetAmount = Math.Max(
                    0,
                    sale.TotalAmount -
                    sale.ReturnedAmount);

                sale.NetProfit =
                    sale.TotalProfit -
                    sale.ReturnedProfitAmount;

                if (sale.PaidAmount > sale.NetAmount)
                {
                    sale.RemainingAmount = 0;
                }
                else
                {
                    sale.RemainingAmount =
                        sale.NetAmount -
                        sale.PaidAmount;
                }

                decimal totalRefundObligation =
                    Math.Max(
                        0,
                        sale.PaidAmount -
                        sale.NetAmount);

                sale.RefundDueAmount = Math.Max(
                    0,
                    totalRefundObligation -
                    sale.CustomerRefundedAmount);

                if (sale.NetAmount == 0 ||
                    sale.RemainingAmount == 0)
                {
                    sale.PaymentStatus =
                        PaymentStatus.Paid;
                }
                else if (sale.PaidAmount == 0)
                {
                    sale.PaymentStatus =
                        PaymentStatus.Unpaid;
                }
                else
                {
                    sale.PaymentStatus =
                        PaymentStatus.PartiallyPaid;
                }
            }
        }
    }
}