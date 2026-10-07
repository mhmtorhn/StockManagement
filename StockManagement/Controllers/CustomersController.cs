using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // Müşterileri listeler, arar ve filtreler
        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            string? searchText,
            CustomerType? customerType,
            bool? isActive)
        {
            IQueryable<Customer> query = _context.Customers
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string normalizedSearchText =
                    searchText.Trim().ToLower();

                query = query.Where(customer =>
                    customer.Name.ToLower()
                        .Contains(normalizedSearchText) ||
                    customer.ContactPerson.ToLower()
                        .Contains(normalizedSearchText) ||
                    customer.PhoneNumber.ToLower()
                        .Contains(normalizedSearchText) ||
                    customer.Email.ToLower()
                        .Contains(normalizedSearchText) ||
                    customer.TaxNumber.ToLower()
                        .Contains(normalizedSearchText));
            }

            if (customerType.HasValue)
            {
                query = query.Where(customer =>
                    customer.CustomerType == customerType.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(customer =>
                    customer.IsActive == isActive.Value);
            }

            var customers = await query
                .OrderByDescending(customer => customer.Id)
                .Select(customer => new
                {
                    customer.Id,
                    customer.CustomerType,
                    customer.Name,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.Address,
                    customer.TaxNumber,
                    customer.Notes,
                    customer.IsActive,
                    customer.CreatedAt
                })
                .ToListAsync();

            return Ok(customers);
        }

        // ID numarasına göre müşteri detayını getirir
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(customer => customer.Id == id)
                .Select(customer => new
                {
                    customer.Id,
                    customer.CustomerType,
                    customer.Name,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.Address,
                    customer.TaxNumber,
                    customer.Notes,
                    customer.IsActive,
                    customer.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (customer is null)
            {
                return NotFound(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            return Ok(customer);
        }

        // Yeni müşteri oluşturur
        [HttpPost]
        public async Task<IActionResult> CreateCustomer(
            CreateCustomerDto dto)
        {
            if (!Enum.IsDefined(
                    typeof(CustomerType),
                    dto.CustomerType))
            {
                return BadRequest(new
                {
                    Message = "Geçersiz müşteri türü."
                });
            }

            string name = dto.Name.Trim();
            string contactPerson = dto.ContactPerson.Trim();
            string phoneNumber = dto.PhoneNumber.Trim();
            string email = dto.Email.Trim().ToLower();
            string address = dto.Address.Trim();
            string taxNumber = dto.TaxNumber.Trim();
            string notes = dto.Notes.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    Message = "Müşteri adı zorunludur."
                });
            }

            if (dto.CustomerType == CustomerType.Corporate &&
                string.IsNullOrWhiteSpace(taxNumber))
            {
                return BadRequest(new
                {
                    Message =
                        "Kurumsal müşterilerde vergi numarası zorunludur."
                });
            }

            if (!string.IsNullOrWhiteSpace(taxNumber))
            {
                bool taxNumberExists = await _context.Customers
                    .AnyAsync(customer =>
                        customer.TaxNumber == taxNumber);

                if (taxNumberExists)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Bu vergi numarasına ait müşteri zaten bulunuyor."
                    });
                }
            }

            var customer = new Customer
            {
                CustomerType = dto.CustomerType,
                Name = name,
                ContactPerson = contactPerson,
                PhoneNumber = phoneNumber,
                Email = email,
                Address = address,
                TaxNumber = taxNumber,
                Notes = notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                Message = "Müşteri başarıyla oluşturuldu.",
                Customer = new
                {
                    customer.Id,
                    customer.CustomerType,
                    customer.Name,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.Address,
                    customer.TaxNumber,
                    customer.Notes,
                    customer.IsActive,
                    customer.CreatedAt
                }
            });
        }

        // Müşteri bilgilerini günceller
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(
            int id,
            UpdateCustomerDto dto)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(customer =>
                    customer.Id == id);

            if (customer is null)
            {
                return NotFound(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            if (!Enum.IsDefined(
                    typeof(CustomerType),
                    dto.CustomerType))
            {
                return BadRequest(new
                {
                    Message = "Geçersiz müşteri türü."
                });
            }

            string name = dto.Name.Trim();
            string contactPerson = dto.ContactPerson.Trim();
            string phoneNumber = dto.PhoneNumber.Trim();
            string email = dto.Email.Trim().ToLower();
            string address = dto.Address.Trim();
            string taxNumber = dto.TaxNumber.Trim();
            string notes = dto.Notes.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new
                {
                    Message = "Müşteri adı zorunludur."
                });
            }

            if (dto.CustomerType == CustomerType.Corporate &&
                string.IsNullOrWhiteSpace(taxNumber))
            {
                return BadRequest(new
                {
                    Message =
                        "Kurumsal müşterilerde vergi numarası zorunludur."
                });
            }

            if (!string.IsNullOrWhiteSpace(taxNumber))
            {
                bool taxNumberExists = await _context.Customers
                    .AnyAsync(otherCustomer =>
                        otherCustomer.Id != id &&
                        otherCustomer.TaxNumber == taxNumber);

                if (taxNumberExists)
                {
                    return BadRequest(new
                    {
                        Message =
                            "Bu vergi numarasına ait başka bir müşteri zaten bulunuyor."
                    });
                }
            }

            customer.CustomerType = dto.CustomerType;
            customer.Name = name;
            customer.ContactPerson = contactPerson;
            customer.PhoneNumber = phoneNumber;
            customer.Email = email;
            customer.Address = address;
            customer.TaxNumber = taxNumber;
            customer.Notes = notes;
            customer.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Müşteri başarıyla güncellendi.",
                Customer = new
                {
                    customer.Id,
                    customer.CustomerType,
                    customer.Name,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.Address,
                    customer.TaxNumber,
                    customer.Notes,
                    customer.IsActive,
                    customer.CreatedAt
                }
            });
        }

        // Müşteriyi pasif duruma getirir
        [HttpPut("{id:int}/deactivate")]
        public async Task<IActionResult> DeactivateCustomer(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(customer =>
                    customer.Id == id);

            if (customer is null)
            {
                return NotFound(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            if (!customer.IsActive)
            {
                return BadRequest(new
                {
                    Message = "Müşteri zaten pasif durumda."
                });
            }

            customer.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Müşteri pasif duruma getirildi.",
                customer.Id,
                customer.Name,
                customer.IsActive
            });
        }

        // Müşteriyi aktif duruma getirir
        [HttpPut("{id:int}/activate")]
        public async Task<IActionResult> ActivateCustomer(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(customer =>
                    customer.Id == id);

            if (customer is null)
            {
                return NotFound(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            if (customer.IsActive)
            {
                return BadRequest(new
                {
                    Message = "Müşteri zaten aktif durumda."
                });
            }

            customer.IsActive = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Müşteri aktif duruma getirildi.",
                customer.Id,
                customer.Name,
                customer.IsActive
            });
        }
    }
}