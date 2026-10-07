using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/Sales")]
    [ApiController]
    public class SaleCancellationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleCancellationsController(
            AppDbContext context)
        {
            _context = context;
        }

        // Taslak satışı stokları etkilemeden iptal eder
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> CancelSale(
            int id,
            CancelSaleDto dto)
        {
            var sale = await _context.Sales
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
                        "Onaylanmış satış bu işlemle iptal edilemez. Satış iadesi oluşturulmalıdır."
                });
            }

            if (sale.Status == SaleStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış kaydı zaten iptal edilmiş."
                });
            }

            if (sale.Status != SaleStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca taslak satış kayıtları iptal edilebilir."
                });
            }

            string cancellationReason =
                dto.CancellationReason.Trim();

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                return BadRequest(new
                {
                    Message = "İptal nedeni zorunludur."
                });
            }

            sale.Status = SaleStatus.Cancelled;
            sale.CancelledAt = DateTime.UtcNow;
            sale.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Taslak satış başarıyla iptal edildi.",
                sale.Id,
                sale.InvoiceNumber,
                sale.Status,
                sale.CancelledAt,
                sale.CancellationReason
            });
        }
    }
}