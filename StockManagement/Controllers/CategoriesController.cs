using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // Bütün kategorileri listeler
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderByDescending(category => category.Id)
                .Select(category => new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.IsActive,
                    category.CreatedAt,
                    ProductCount = category.Products.Count
                })
                .ToListAsync();

            return Ok(categories);
        }

        // ID numarasına göre tek bir kategori getirir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Where(category => category.Id == id)
                .Select(category => new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.IsActive,
                    category.CreatedAt,
                    ProductCount = category.Products.Count
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound("Kategori bulunamadı.");
            }

            return Ok(category);
        }

        // Yeni kategori oluşturur
        [HttpPost]
        public async Task<IActionResult> Create(CreateCategoryDto dto)
        {
            string categoryName = dto.Name.Trim();

            bool categoryExists = await _context.Categories
                .AnyAsync(category =>
                    category.Name.ToLower() ==
                    categoryName.ToLower());

            if (categoryExists)
            {
                return BadRequest(
                    "Bu isimde bir kategori zaten bulunuyor.");
            }

            var category = new Category
            {
                Name = categoryName,
                Description = dto.Description.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt
            });
        }

        // Kategori bilgilerini günceller
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound("Kategori bulunamadı.");
            }

            string categoryName = dto.Name.Trim();

            bool categoryExists = await _context.Categories
                .AnyAsync(otherCategory =>
                    otherCategory.Name.ToLower() ==
                    categoryName.ToLower() &&
                    otherCategory.Id != id);

            if (categoryExists)
            {
                return BadRequest(
                    "Bu isimde başka bir kategori bulunuyor.");
            }

            category.Name = categoryName;
            category.Description = dto.Description.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt
            });
        }

        // Kategoriyi pasif hale getirir
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound("Kategori bulunamadı.");
            }

            if (!category.IsActive)
            {
                return BadRequest("Kategori zaten pasif durumda.");
            }

            category.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Kategori pasif hale getirildi.",
                category.Id,
                category.Name,
                category.IsActive
            });
        }

        // Pasif kategoriyi tekrar aktif hale getirir
        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound("Kategori bulunamadı.");
            }

            if (category.IsActive)
            {
                return BadRequest("Kategori zaten aktif durumda.");
            }

            category.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Kategori aktif hale getirildi.",
                category.Id,
                category.Name,
                category.IsActive
            });
        }
    }
}