using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [ApiController]
    [Route("api/SaleReturns")]
    public class SaleReturnCancellationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleReturnCancellationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> CancelSaleReturn(
            int id,
            CancelSaleReturnDto dto)
        {
            var saleReturn = await _context.SaleReturns
                .Include(saleReturn => saleReturn.Sale)
                .FirstOrDefaultAsync(saleReturn => saleReturn.Id == id);

            if (saleReturn == null)
            {
                return NotFound(new
                {
                    message = "Satış iadesi bulunamadı."
                });
            }

            if (saleReturn.Status == SaleReturnStatus.Approved)
            {
                return BadRequest(new
                {
                    message = "Onaylanmış satış iadesi iptal edilemez."
                });
            }

            if (saleReturn.Status == SaleReturnStatus.Cancelled)
            {
                return BadRequest(new
                {
                    message = "Satış iadesi zaten iptal edilmiş."
                });
            }

            if (saleReturn.Status != SaleReturnStatus.Draft)
            {
                return BadRequest(new
                {
                    message = "Yalnızca taslak satış iadeleri iptal edilebilir."
                });
            }

            string cancellationReason = dto.CancellationReason.Trim();

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                return BadRequest(new
                {
                    message = "İade iptal nedeni zorunludur."
                });
            }

            saleReturn.Status = SaleReturnStatus.Cancelled;
            saleReturn.CancelledAt = DateTime.UtcNow;
            saleReturn.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Taslak satış iadesi başarıyla iptal edildi.",

                saleReturn = new
                {
                    saleReturn.Id,
                    saleReturn.SaleId,
                    saleReturn.Sale.InvoiceNumber,
                    saleReturn.ReturnNumber,
                    saleReturn.Status,
                    saleReturn.CancelledAt,
                    saleReturn.CancellationReason
                }
            });
        }
    }
}