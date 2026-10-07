using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuppliersController(AppDbContext context)
        {
            _context = context;
        }

        // Tedarikçileri listeler ve filtreler
        [HttpGet]
        public async Task<IActionResult> GetSuppliers(
            string? searchText,
            bool? isActive)
        {
            IQueryable<Supplier> query = _context.Suppliers
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string normalizedSearchText =
                    searchText.Trim().ToLower();

                query = query.Where(supplier =>
                    supplier.Name.ToLower()
                        .Contains(normalizedSearchText) ||
                    (supplier.ContactPerson != null &&
                     supplier.ContactPerson.ToLower()
                         .Contains(normalizedSearchText)) ||
                    (supplier.PhoneNumber != null &&
                     supplier.PhoneNumber.ToLower()
                         .Contains(normalizedSearchText)) ||
                    (supplier.Email != null &&
                     supplier.Email.ToLower()
                         .Contains(normalizedSearchText)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(supplier =>
                    supplier.IsActive == isActive.Value);
            }

            var suppliers = await query
                .OrderByDescending(supplier => supplier.Id)
                .Select(supplier => new
                {
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.Address,
                    supplier.IsActive,
                    supplier.CreatedAt,
                    ProductCount = supplier.Products.Count
                })
                .ToListAsync();

            return Ok(suppliers);
        }

        // ID numarasına göre tek bir tedarikçi getirir
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(supplier => supplier.Id == id)
                .Select(supplier => new
                {
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.Address,
                    supplier.IsActive,
                    supplier.CreatedAt,
                    ProductCount = supplier.Products.Count
                })
                .FirstOrDefaultAsync();

            if (supplier is null)
            {
                return NotFound(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            return Ok(supplier);
        }

        // Tedarikçiye bağlı ürünleri listeler
        [HttpGet("{id:int}/products")]
        public async Task<IActionResult> GetSupplierProducts(int id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(supplier => supplier.Id == id)
                .Select(supplier => new
                {
                    supplier.Id,
                    supplier.Name,
                    Products = supplier.Products
                        .OrderByDescending(product => product.Id)
                        .Select(product => new
                        {
                            product.Id,
                            product.Name,
                            product.Barcode,
                            product.PurchasePrice,
                            product.SalePrice,
                            product.StockQuantity,
                            product.MinimumStockLevel,
                            product.IsActive,
                            product.CategoryId,
                            CategoryName = product.Category != null
                                ? product.Category.Name
                                : null
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (supplier is null)
            {
                return NotFound(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            return Ok(new
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                ProductCount = supplier.Products.Count,
                supplier.Products
            });
        }

        // Yeni tedarikçi oluşturur
        [HttpPost]
        public async Task<IActionResult> CreateSupplier(
            CreateSupplierDto createSupplierDto)
        {
            if (string.IsNullOrWhiteSpace(createSupplierDto.Name))
            {
                return BadRequest(new
                {
                    Message = "Tedarikçi adı zorunludur."
                });
            }

            var supplier = new Supplier
            {
                Name = createSupplierDto.Name.Trim(),
                ContactPerson =
                    createSupplierDto.ContactPerson?.Trim(),
                PhoneNumber =
                    createSupplierDto.PhoneNumber?.Trim(),
                Email =
                    createSupplierDto.Email?.Trim(),
                Address =
                    createSupplierDto.Address?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Suppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                Message = "Tedarikçi başarıyla oluşturuldu.",
                Supplier = new
                {
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.Address,
                    supplier.IsActive,
                    supplier.CreatedAt,
                    ProductCount = 0
                }
            });
        }

        // Tedarikçi bilgilerini günceller
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSupplier(
            int id,
            UpdateSupplierDto updateSupplierDto)
        {
            if (string.IsNullOrWhiteSpace(updateSupplierDto.Name))
            {
                return BadRequest(new
                {
                    Message = "Tedarikçi adı zorunludur."
                });
            }

            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier is null)
            {
                return NotFound(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            supplier.Name = updateSupplierDto.Name.Trim();
            supplier.ContactPerson =
                updateSupplierDto.ContactPerson?.Trim();
            supplier.PhoneNumber =
                updateSupplierDto.PhoneNumber?.Trim();
            supplier.Email =
                updateSupplierDto.Email?.Trim();
            supplier.Address =
                updateSupplierDto.Address?.Trim();

            await _context.SaveChangesAsync();

            int productCount = await _context.Products
                .CountAsync(product =>
                    product.SupplierId == supplier.Id);

            return Ok(new
            {
                Message = "Tedarikçi başarıyla güncellendi.",
                Supplier = new
                {
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.Address,
                    supplier.IsActive,
                    supplier.CreatedAt,
                    ProductCount = productCount
                }
            });
        }

        // Tedarikçiyi pasif hale getirir
        [HttpPut("{id:int}/deactivate")]
        public async Task<IActionResult> DeactivateSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier is null)
            {
                return NotFound(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            if (!supplier.IsActive)
            {
                return BadRequest(new
                {
                    Message = "Tedarikçi zaten pasif durumda."
                });
            }

            supplier.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Tedarikçi başarıyla pasif hale getirildi.",
                supplier.Id,
                supplier.Name,
                supplier.IsActive
            });
        }

        // Pasif tedarikçiyi tekrar aktif hale getirir
        [HttpPut("{id:int}/activate")]
        public async Task<IActionResult> ActivateSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier is null)
            {
                return NotFound(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            if (supplier.IsActive)
            {
                return BadRequest(new
                {
                    Message = "Tedarikçi zaten aktif durumda."
                });
            }

            supplier.IsActive = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Tedarikçi başarıyla aktif hale getirildi.",
                supplier.Id,
                supplier.Name,
                supplier.IsActive
            });
        }
    }
}