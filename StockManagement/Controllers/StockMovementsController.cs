using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockMovementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockMovementsController(AppDbContext context)
        {
            _context = context;
        }

        // Bütün stok hareketlerini listeler
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stockMovements = await _context.StockMovements
                .AsNoTracking()
                .OrderByDescending(stockMovement => stockMovement.Id)
                .Select(stockMovement => new
                {
                    stockMovement.Id,
                    stockMovement.ProductId,
                    ProductName = stockMovement.Product.Name,
                    ProductBarcode = stockMovement.Product.Barcode,
                    stockMovement.MovementType,
                    stockMovement.Quantity,
                    stockMovement.Description,
                    stockMovement.PreviousStock,
                    stockMovement.CurrentStock,
                    stockMovement.CreatedAt
                })
                .ToListAsync();

            return Ok(stockMovements);
        }

        // Belirli bir ürüne ait stok hareketlerini listeler
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            bool productExists = await _context.Products
                .AnyAsync(product => product.Id == productId);

            if (!productExists)
            {
                return NotFound("Ürün bulunamadı.");
            }

            var stockMovements = await _context.StockMovements
                .AsNoTracking()
                .Where(stockMovement =>
                    stockMovement.ProductId == productId)
                .OrderByDescending(stockMovement => stockMovement.Id)
                .Select(stockMovement => new
                {
                    stockMovement.Id,
                    stockMovement.ProductId,
                    ProductName = stockMovement.Product.Name,
                    ProductBarcode = stockMovement.Product.Barcode,
                    stockMovement.MovementType,
                    stockMovement.Quantity,
                    stockMovement.Description,
                    stockMovement.PreviousStock,
                    stockMovement.CurrentStock,
                    stockMovement.CreatedAt
                })
                .ToListAsync();

            return Ok(stockMovements);
        }

        // Ürüne stok girişi yapar
        [HttpPost("stock-in")]
        public async Task<IActionResult> StockIn(
            CreateStockMovementDto dto)
        {
            var product = await _context.Products
                .FindAsync(dto.ProductId);

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            if (!product.IsActive)
            {
                return BadRequest(
                    "Pasif durumdaki ürüne stok girişi yapılamaz.");
            }

            int previousStock = product.StockQuantity;

            product.StockQuantity += dto.Quantity;

            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                MovementType = "Giriş",
                Quantity = dto.Quantity,
                Description = dto.Description,
                PreviousStock = previousStock,
                CurrentStock = product.StockQuantity
            };

            _context.StockMovements.Add(stockMovement);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                Message = "Stok girişi başarıyla yapıldı.",
                StockMovementId = stockMovement.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                stockMovement.MovementType,
                stockMovement.Quantity,
                stockMovement.PreviousStock,
                stockMovement.CurrentStock,
                stockMovement.Description,
                stockMovement.CreatedAt
            });
        }

        // Üründen stok çıkışı yapar
        [HttpPost("stock-out")]
        public async Task<IActionResult> StockOut(
            CreateStockMovementDto dto)
        {
            var product = await _context.Products
                .FindAsync(dto.ProductId);

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            if (!product.IsActive)
            {
                return BadRequest(
                    "Pasif durumdaki üründen stok çıkışı yapılamaz.");
            }

            if (product.StockQuantity < dto.Quantity)
            {
                return BadRequest(
                    $"Yetersiz stok. Mevcut stok: {product.StockQuantity}");
            }

            int previousStock = product.StockQuantity;

            product.StockQuantity -= dto.Quantity;

            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                MovementType = "Çıkış",
                Quantity = dto.Quantity,
                Description = dto.Description,
                PreviousStock = previousStock,
                CurrentStock = product.StockQuantity
            };

            _context.StockMovements.Add(stockMovement);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                Message = "Stok çıkışı başarıyla yapıldı.",
                StockMovementId = stockMovement.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                stockMovement.MovementType,
                stockMovement.Quantity,
                stockMovement.PreviousStock,
                stockMovement.CurrentStock,
                stockMovement.Description,
                stockMovement.CreatedAt
            });
        }
    }
}