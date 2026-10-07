using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // Ürünleri listeler ve filtreler
        [HttpGet]
        public async Task<IActionResult> GetAll(
            string? searchText,
            int? categoryId,
            int? supplierId,
            bool? isActive)
        {
            IQueryable<Product> query = _context.Products
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string normalizedSearchText =
                    searchText.Trim().ToLower();

                query = query.Where(product =>
                    product.Name.ToLower()
                        .Contains(normalizedSearchText) ||
                    product.Barcode.ToLower()
                        .Contains(normalizedSearchText));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(product =>
                    product.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue)
            {
                query = query.Where(product =>
                    product.SupplierId == supplierId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(product =>
                    product.IsActive == isActive.Value);
            }

            var products = await query
                .OrderByDescending(product => product.Id)
                .Select(product => new
                {
                    product.Id,
                    product.Name,
                    product.Barcode,
                    product.Description,
                    product.PurchasePrice,
                    product.SalePrice,
                    product.StockQuantity,
                    product.MinimumStockLevel,
                    product.IsActive,
                    product.CreatedAt,
                    product.CategoryId,
                    CategoryName = product.Category != null
                        ? product.Category.Name
                        : null,
                    product.SupplierId,
                    SupplierName = product.Supplier != null
                        ? product.Supplier.Name
                        : null
                })
                .ToListAsync();

            return Ok(products);
        }

        // Minimum stok seviyesine ulaşan veya altına düşen ürünleri listeler
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockProducts()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(product =>
                    product.IsActive &&
                    product.StockQuantity <= product.MinimumStockLevel)
                .OrderBy(product => product.StockQuantity)
                .Select(product => new
                {
                    product.Id,
                    product.Name,
                    product.Barcode,
                    product.CategoryId,
                    CategoryName = product.Category != null
                        ? product.Category.Name
                        : null,
                    product.SupplierId,
                    SupplierName = product.Supplier != null
                        ? product.Supplier.Name
                        : null,
                    product.StockQuantity,
                    product.MinimumStockLevel,
                    RequiredQuantity =
                        product.MinimumStockLevel - product.StockQuantity,
                    Status = product.StockQuantity == 0
                        ? "Stok tükendi"
                        : "Düşük stok"
                })
                .ToListAsync();

            return Ok(products);
        }

        // ID numarasına göre tek bir ürün getirir
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(product => product.Id == id)
                .Select(product => new
                {
                    product.Id,
                    product.Name,
                    product.Barcode,
                    product.Description,
                    product.PurchasePrice,
                    product.SalePrice,
                    product.StockQuantity,
                    product.MinimumStockLevel,
                    product.IsActive,
                    product.CreatedAt,
                    product.CategoryId,
                    CategoryName = product.Category != null
                        ? product.Category.Name
                        : null,
                    product.SupplierId,
                    SupplierName = product.Supplier != null
                        ? product.Supplier.Name
                        : null
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            return Ok(product);
        }

        // Yeni ürünü sıfır stokla oluşturur
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            string barcode = dto.Barcode.Trim();

            bool barcodeExists = await _context.Products
                .AnyAsync(product => product.Barcode == barcode);

            if (barcodeExists)
            {
                return BadRequest(
                    "Bu barkoda sahip bir ürün zaten bulunuyor.");
            }

            if (dto.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FindAsync(dto.CategoryId.Value);

                if (category == null)
                {
                    return BadRequest("Kategori bulunamadı.");
                }

                if (!category.IsActive)
                {
                    return BadRequest(
                        "Pasif bir kategori ürüne atanamaz.");
                }
            }

            if (dto.SupplierId.HasValue)
            {
                var supplier = await _context.Suppliers
                    .FindAsync(dto.SupplierId.Value);

                if (supplier == null)
                {
                    return BadRequest("Tedarikçi bulunamadı.");
                }

                if (!supplier.IsActive)
                {
                    return BadRequest(
                        "Pasif bir tedarikçi ürüne atanamaz.");
                }
            }

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Barcode = barcode,
                Description = dto.Description.Trim(),
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                StockQuantity = 0,
                MinimumStockLevel = dto.MinimumStockLevel,
                CategoryId = dto.CategoryId,
                SupplierId = dto.SupplierId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            string? categoryName = null;
            string? supplierName = null;

            if (product.CategoryId.HasValue)
            {
                categoryName = await _context.Categories
                    .Where(category =>
                        category.Id == product.CategoryId.Value)
                    .Select(category => category.Name)
                    .FirstAsync();
            }

            if (product.SupplierId.HasValue)
            {
                supplierName = await _context.Suppliers
                    .Where(supplier =>
                        supplier.Id == product.SupplierId.Value)
                    .Select(supplier => supplier.Name)
                    .FirstAsync();
            }

            return StatusCode(201, new
            {
                product.Id,
                product.Name,
                product.Barcode,
                product.Description,
                product.PurchasePrice,
                product.SalePrice,
                product.StockQuantity,
                product.MinimumStockLevel,
                product.IsActive,
                product.CreatedAt,
                product.CategoryId,
                CategoryName = categoryName,
                product.SupplierId,
                SupplierName = supplierName
            });
        }

        // Ürün bilgilerini günceller
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            string barcode = dto.Barcode.Trim();

            bool barcodeExists = await _context.Products
                .AnyAsync(otherProduct =>
                    otherProduct.Barcode == barcode &&
                    otherProduct.Id != id);

            if (barcodeExists)
            {
                return BadRequest(
                    "Bu barkoda sahip başka bir ürün bulunuyor.");
            }

            if (dto.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FindAsync(dto.CategoryId.Value);

                if (category == null)
                {
                    return BadRequest("Kategori bulunamadı.");
                }

                if (!category.IsActive)
                {
                    return BadRequest(
                        "Pasif bir kategori ürüne atanamaz.");
                }
            }

            if (dto.SupplierId.HasValue)
            {
                var supplier = await _context.Suppliers
                    .FindAsync(dto.SupplierId.Value);

                if (supplier == null)
                {
                    return BadRequest("Tedarikçi bulunamadı.");
                }

                if (!supplier.IsActive)
                {
                    return BadRequest(
                        "Pasif bir tedarikçi ürüne atanamaz.");
                }
            }

            product.Name = dto.Name.Trim();
            product.Barcode = barcode;
            product.Description = dto.Description.Trim();
            product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice;
            product.MinimumStockLevel = dto.MinimumStockLevel;
            product.CategoryId = dto.CategoryId;
            product.SupplierId = dto.SupplierId;

            await _context.SaveChangesAsync();

            string? categoryName = null;
            string? supplierName = null;

            if (product.CategoryId.HasValue)
            {
                categoryName = await _context.Categories
                    .Where(category =>
                        category.Id == product.CategoryId.Value)
                    .Select(category => category.Name)
                    .FirstAsync();
            }

            if (product.SupplierId.HasValue)
            {
                supplierName = await _context.Suppliers
                    .Where(supplier =>
                        supplier.Id == product.SupplierId.Value)
                    .Select(supplier => supplier.Name)
                    .FirstAsync();
            }

            return Ok(new
            {
                product.Id,
                product.Name,
                product.Barcode,
                product.Description,
                product.PurchasePrice,
                product.SalePrice,
                product.StockQuantity,
                product.MinimumStockLevel,
                product.IsActive,
                product.CreatedAt,
                product.CategoryId,
                CategoryName = categoryName,
                product.SupplierId,
                SupplierName = supplierName
            });
        }

        // Ürünü pasif hale getirir
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            if (!product.IsActive)
            {
                return BadRequest("Ürün zaten pasif durumda.");
            }

            product.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Ürün pasif hale getirildi.",
                product.Id,
                product.Name,
                product.IsActive
            });
        }

        // Pasif ürünü tekrar aktif hale getirir
        [HttpPatch("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            if (product.IsActive)
            {
                return BadRequest("Ürün zaten aktif durumda.");
            }

            product.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Ürün aktif hale getirildi.",
                product.Id,
                product.Name,
                product.IsActive
            });
        }
    }
}