using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // Dashboard için genel stok istatistiklerini getirir
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            int totalProductCount = await _context.Products
                .CountAsync();

            int activeProductCount = await _context.Products
                .CountAsync(product => product.IsActive);

            int passiveProductCount = await _context.Products
                .CountAsync(product => !product.IsActive);

            int lowStockProductCount = await _context.Products
                .CountAsync(product =>
                    product.IsActive &&
                    product.StockQuantity > 0 &&
                    product.StockQuantity <=
                    product.MinimumStockLevel);

            int outOfStockProductCount = await _context.Products
                .CountAsync(product =>
                    product.IsActive &&
                    product.StockQuantity == 0);

            int totalStockQuantity = await _context.Products
                .Where(product => product.IsActive)
                .SumAsync(product => product.StockQuantity);

            int totalCategoryCount = await _context.Categories
                .CountAsync();

            int totalStockMovementCount =
                await _context.StockMovements.CountAsync();

            int totalSupplierCount = await _context.Suppliers
                .CountAsync();

            int activeSupplierCount = await _context.Suppliers
                .CountAsync(supplier => supplier.IsActive);

            int passiveSupplierCount = await _context.Suppliers
                .CountAsync(supplier => !supplier.IsActive);

            int supplierWithoutProductCount =
                await _context.Suppliers
                    .CountAsync(supplier =>
                        !supplier.Products.Any());

            return Ok(new
            {
                TotalProductCount = totalProductCount,
                ActiveProductCount = activeProductCount,
                PassiveProductCount = passiveProductCount,
                LowStockProductCount = lowStockProductCount,
                OutOfStockProductCount = outOfStockProductCount,
                TotalStockQuantity = totalStockQuantity,
                TotalCategoryCount = totalCategoryCount,
                TotalStockMovementCount =
                    totalStockMovementCount,
                TotalSupplierCount = totalSupplierCount,
                ActiveSupplierCount = activeSupplierCount,
                PassiveSupplierCount = passiveSupplierCount,
                SupplierWithoutProductCount =
                    supplierWithoutProductCount
            });
        }
    }
}