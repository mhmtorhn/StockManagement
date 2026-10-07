using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/Sales")]
    [ApiController]
    public class SalePaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalePaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // Onaylanmış satışa tahsilat ekler
        [HttpPost("{id:int}/payments")]
        public async Task<IActionResult> AddSalePayment(
            int id,
            CreateSalePaymentDto dto)
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

            if (sale.Status == SaleStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Taslak satışa tahsilat eklenemez. Önce satış onaylanmalıdır."
                });
            }

            if (sale.Status == SaleStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satışa tahsilat eklenemez."
                });
            }

            if (sale.Status != SaleStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca onaylanmış satışlara tahsilat eklenebilir."
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

            if (dto.Amount <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Tahsilat tutarı 0'dan büyük olmalıdır."
                });
            }

            if (sale.PaymentStatus == PaymentStatus.Paid ||
                sale.RemainingAmount <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu satışın müşteri borcu tamamen tahsil edilmiş."
                });
            }

            if (dto.Amount > sale.RemainingAmount)
            {
                return BadRequest(new
                {
                    Message =
                        $"Tahsilat tutarı kalan müşteri borcundan fazla olamaz. Kalan borç: {sale.RemainingAmount}"
                });
            }

            var payment = new SalePayment
            {
                SaleId = sale.Id,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                PaymentDate =
                    dto.PaymentDate ?? DateTime.UtcNow,
                ReferenceNumber =
                    dto.ReferenceNumber.Trim(),
                Description = dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsCancelled = false
            };

            sale.PaidAmount += payment.Amount;
            sale.RemainingAmount =
                sale.TotalAmount - sale.PaidAmount;

            if (sale.RemainingAmount == 0)
            {
                sale.PaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                sale.PaymentStatus =
                    PaymentStatus.PartiallyPaid;
            }

            await _context.SalePayments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                Message = sale.PaymentStatus ==
                          PaymentStatus.Paid
                    ? "Tahsilat kaydedildi. Müşteri borcu tamamen kapandı."
                    : "Kısmi tahsilat başarıyla kaydedildi.",

                Payment = new
                {
                    payment.Id,
                    payment.SaleId,
                    payment.Amount,
                    payment.PaymentMethod,
                    payment.PaymentDate,
                    payment.ReferenceNumber,
                    payment.Description,
                    payment.CreatedAt
                },

                SaleSummary = new
                {
                    sale.Id,
                    sale.InvoiceNumber,
                    sale.TotalAmount,
                    sale.PaidAmount,
                    sale.RemainingAmount,
                    sale.PaymentStatus
                }
            });
        }

        // Satışın tahsilat geçmişini listeler
        [HttpGet("{id:int}/payments")]
        public async Task<IActionResult> GetSalePayments(int id)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Where(sale => sale.Id == id)
                .Select(sale => new
                {
                    sale.Id,
                    sale.InvoiceNumber,
                    sale.CustomerId,
                    CustomerName = sale.Customer.Name,
                    sale.SaleDate,
                    sale.DueDate,
                    sale.TotalAmount,
                    sale.PaidAmount,
                    sale.RemainingAmount,
                    sale.PaymentStatus,

                    Payments = sale.SalePayments
                        .OrderByDescending(payment =>
                            payment.PaymentDate)
                        .ThenByDescending(payment =>
                            payment.Id)
                        .Select(payment => new
                        {
                            payment.Id,
                            payment.Amount,
                            payment.PaymentMethod,
                            payment.PaymentDate,
                            payment.ReferenceNumber,
                            payment.Description,
                            payment.CreatedAt,
                            payment.IsCancelled,
                            payment.CancelledAt,
                            payment.CancellationReason
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (sale is null)
            {
                return NotFound(new
                {
                    Message = "Satış kaydı bulunamadı."
                });
            }

            return Ok(sale);
        }

        // Tahsilatı silmeden iptal eder ve müşteri borcunu hesaplar
        [HttpPut("{id:int}/payments/{paymentId:int}/cancel")]
        public async Task<IActionResult> CancelSalePayment(
            int id,
            int paymentId,
            CancelSalePaymentDto dto)
        {
            var sale = await _context.Sales
                .Include(sale => sale.SalePayments)
                .FirstOrDefaultAsync(sale =>
                    sale.Id == id);

            if (sale is null)
            {
                return NotFound(new
                {
                    Message = "Satış kaydı bulunamadı."
                });
            }

            var payment = sale.SalePayments
                .FirstOrDefault(payment =>
                    payment.Id == paymentId);

            if (payment is null)
            {
                return NotFound(new
                {
                    Message =
                        "Bu satışa ait tahsilat kaydı bulunamadı."
                });
            }

            if (payment.IsCancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "Tahsilat kaydı zaten iptal edilmiş."
                });
            }

            string cancellationReason =
                dto.CancellationReason.Trim();

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                return BadRequest(new
                {
                    Message =
                        "Tahsilat iptal nedeni zorunludur."
                });
            }

            payment.IsCancelled = true;
            payment.CancelledAt = DateTime.UtcNow;
            payment.CancellationReason = cancellationReason;

            sale.PaidAmount = sale.SalePayments
                .Where(existingPayment =>
                    !existingPayment.IsCancelled)
                .Sum(existingPayment =>
                    existingPayment.Amount);

            sale.RemainingAmount =
                sale.TotalAmount - sale.PaidAmount;

            if (sale.PaidAmount == 0)
            {
                sale.PaymentStatus = PaymentStatus.Unpaid;
            }
            else if (sale.RemainingAmount == 0)
            {
                sale.PaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                sale.PaymentStatus =
                    PaymentStatus.PartiallyPaid;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Tahsilat kaydı başarıyla iptal edildi.",

                Payment = new
                {
                    payment.Id,
                    payment.SaleId,
                    payment.Amount,
                    payment.PaymentMethod,
                    payment.IsCancelled,
                    payment.CancelledAt,
                    payment.CancellationReason
                },

                SaleSummary = new
                {
                    sale.Id,
                    sale.InvoiceNumber,
                    sale.TotalAmount,
                    sale.PaidAmount,
                    sale.RemainingAmount,
                    sale.PaymentStatus
                }
            });
        }
    }
}