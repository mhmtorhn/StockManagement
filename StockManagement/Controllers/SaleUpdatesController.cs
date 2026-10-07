using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/Sales")]
    [ApiController]
    public class SaleUpdatesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleUpdatesController(AppDbContext context)
        {
            _context = context;
        }

        // Taslak satışı stokları etkilemeden günceller
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSale(
            int id,
            UpdateSaleDto dto)
        {
            var sale = await _context.Sales
                .Include(sale => sale.SaleItems)
                .FirstOrDefaultAsync(sale =>
                    sale.Id == id);

            if (sale is null)
            {
                return NotFound(new
                {
                    Message = "Satış kaydı bulunamadı."
                });
            }

            if (sale.Status == SaleStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Onaylanmış satış kaydı güncellenemez."
                });
            }

            if (sale.Status == SaleStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satış kaydı güncellenemez."
                });
            }

            if (sale.Status != SaleStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca taslak satış kayıtları güncellenebilir."
                });
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(customer =>
                    customer.Id == dto.CustomerId);

            if (customer is null)
            {
                return BadRequest(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            if (!customer.IsActive)
            {
                return BadRequest(new
                {
                    Message =
                        "Pasif müşteri için satış güncellenemez."
                });
            }

            string invoiceNumber = dto.InvoiceNumber.Trim();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                return BadRequest(new
                {
                    Message = "Fatura numarası zorunludur."
                });
            }

            bool invoiceExists = await _context.Sales
                .AnyAsync(otherSale =>
                    otherSale.Id != id &&
                    otherSale.InvoiceNumber ==
                        invoiceNumber);

            if (invoiceExists)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu fatura numarasına ait başka bir satış zaten bulunuyor."
                });
            }

            if (!Enum.IsDefined(
                    typeof(PaymentMethod),
                    dto.PaymentMethod))
            {
                return BadRequest(new
                {
                    Message = "Geçersiz ödeme yöntemi."
                });
            }

            DateTime updatedSaleDate =
                dto.SaleDate ?? sale.SaleDate;

            if (dto.DueDate.HasValue &&
                dto.DueDate.Value < updatedSaleDate)
            {
                return BadRequest(new
                {
                    Message =
                        "Vade tarihi satış tarihinden önce olamaz."
                });
            }

            if (dto.Items.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış işleminde en az bir ürün olmalıdır."
                });
            }

            bool hasDuplicateProduct = dto.Items
                .GroupBy(item => item.ProductId)
                .Any(group => group.Count() > 1);

            if (hasDuplicateProduct)
            {
                return BadRequest(new
                {
                    Message =
                        "Aynı ürün satış listesine birden fazla kez eklenemez."
                });
            }

            var productIds = dto.Items
                .Select(item => item.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(product =>
                    productIds.Contains(product.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış listesindeki ürünlerden biri bulunamadı."
                });
            }

            var passiveProduct = products
                .FirstOrDefault(product =>
                    !product.IsActive);

            if (passiveProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{passiveProduct.Name}' ürünü pasif durumdadır."
                });
            }

            sale.CustomerId = dto.CustomerId;
            sale.InvoiceNumber = invoiceNumber;
            sale.SaleDate = updatedSaleDate;
            sale.DueDate = dto.DueDate;
            sale.PaymentMethod = dto.PaymentMethod;
            sale.Description = dto.Description.Trim();

            var existingItems = sale.SaleItems.ToList();

            _context.RemoveRange(existingItems);
            sale.SaleItems.Clear();

            foreach (var itemDto in dto.Items)
            {
                var product = products.First(product =>
                    product.Id == itemDto.ProductId);

                decimal totalPrice =
                    itemDto.Quantity * itemDto.UnitPrice;

                decimal profitAmount =
                    (itemDto.UnitPrice -
                        product.PurchasePrice) *
                    itemDto.Quantity;

                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    CostPrice = product.PurchasePrice,
                    TotalPrice = totalPrice,
                    ProfitAmount = profitAmount
                };

                sale.SaleItems.Add(saleItem);
            }

            sale.TotalAmount = sale.SaleItems
                .Sum(item => item.TotalPrice);

            sale.PaidAmount = 0;
            sale.RemainingAmount = sale.TotalAmount;
            sale.PaymentStatus = PaymentStatus.Unpaid;

            sale.TotalProfit = sale.SaleItems
                .Sum(item => item.ProfitAmount);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Taslak satış başarıyla güncellendi.",
                Sale = new
                {
                    sale.Id,
                    sale.CustomerId,
                    CustomerName = customer.Name,
                    sale.InvoiceNumber,
                    sale.SaleDate,
                    sale.DueDate,
                    sale.TotalAmount,
                    sale.PaidAmount,
                    sale.RemainingAmount,
                    sale.TotalProfit,
                    sale.PaymentMethod,
                    sale.PaymentStatus,
                    sale.Status,
                    sale.Description,
                    sale.CreatedAt,
                    Items = sale.SaleItems.Select(item =>
                    {
                        var product = products.First(product =>
                            product.Id == item.ProductId);

                        return new
                        {
                            item.Id,
                            item.ProductId,
                            ProductName = product.Name,
                            item.Quantity,
                            item.UnitPrice,
                            item.CostPrice,
                            item.TotalPrice,
                            item.ProfitAmount
                        };
                    })
                }
            });
        }
    }
}