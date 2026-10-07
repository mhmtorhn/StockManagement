using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/Sales")]
    [ApiController]
    public class SaleRefundPaymentsController
        : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleRefundPaymentsController(
            AppDbContext context)
        {
            _context = context;
        }

        // Müşteriye para iadesi kaydeder
        [HttpPost("{id:int}/refund-payments")]
        public async Task<IActionResult> AddSaleRefundPayment(
            int id,
            CreateSaleRefundPaymentDto dto)
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

            if (sale.Status != SaleStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca onaylanmış satışlar için müşteriye para iadesi yapılabilir."
                });
            }

            if (sale.RefundDueAmount <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu satış için müşteriye ödenecek para iadesi bulunmuyor."
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
                        "Para iadesi tutarı 0'dan büyük olmalıdır."
                });
            }

            if (dto.Amount > sale.RefundDueAmount)
            {
                return BadRequest(new
                {
                    Message =
                        $"Para iadesi tutarı müşteriye kalan iade borcunu geçemez. Kalan iade borcu: {sale.RefundDueAmount}"
                });
            }

            var refundPayment = new SaleRefundPayment
            {
                SaleId = sale.Id,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                RefundDate =
                    dto.RefundDate ?? DateTime.UtcNow,
                ReferenceNumber =
                    dto.ReferenceNumber.Trim(),
                Description =
                    dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsCancelled = false
            };

            sale.CustomerRefundedAmount +=
                refundPayment.Amount;

            await _context.SaleRefundPayments
                .AddAsync(refundPayment);

            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    Message = sale.RefundDueAmount == 0
                        ? "Müşteriye para iadesi kaydedildi ve iade borcu tamamen kapandı."
                        : "Müşteriye kısmi para iadesi başarıyla kaydedildi.",

                    RefundPayment = new
                    {
                        refundPayment.Id,
                        refundPayment.SaleId,
                        refundPayment.Amount,
                        refundPayment.PaymentMethod,
                        refundPayment.RefundDate,
                        refundPayment.ReferenceNumber,
                        refundPayment.Description,
                        refundPayment.CreatedAt
                    },

                    SaleSummary = new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        sale.TotalAmount,
                        sale.ReturnedAmount,
                        sale.NetAmount,
                        sale.PaidAmount,
                        sale.CustomerRefundedAmount,
                        sale.RefundDueAmount
                    }
                });
        }

        // Satışın müşteriye para iadesi geçmişini getirir
        [HttpGet("{id:int}/refund-payments")]
        public async Task<IActionResult> GetSaleRefundPayments(
            int id)
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
                    sale.TotalAmount,
                    sale.ReturnedAmount,
                    sale.NetAmount,
                    sale.PaidAmount,
                    sale.CustomerRefundedAmount,
                    sale.RefundDueAmount,

                    RefundPayments =
                        sale.SaleRefundPayments
                            .OrderByDescending(payment =>
                                payment.RefundDate)
                            .ThenByDescending(payment =>
                                payment.Id)
                            .Select(payment => new
                            {
                                payment.Id,
                                payment.Amount,
                                payment.PaymentMethod,
                                payment.RefundDate,
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

        // Para iadesini silmeden iptal eder
        [HttpPut(
            "{id:int}/refund-payments/{refundPaymentId:int}/cancel")]
        public async Task<IActionResult> CancelSaleRefundPayment(
            int id,
            int refundPaymentId,
            CancelSaleRefundPaymentDto dto)
        {
            var sale = await _context.Sales
                .Include(sale =>
                    sale.SaleRefundPayments)
                .FirstOrDefaultAsync(sale =>
                    sale.Id == id);

            if (sale is null)
            {
                return NotFound(new
                {
                    Message = "Satış kaydı bulunamadı."
                });
            }

            var refundPayment =
                sale.SaleRefundPayments
                    .FirstOrDefault(payment =>
                        payment.Id == refundPaymentId);

            if (refundPayment is null)
            {
                return NotFound(new
                {
                    Message =
                        "Bu satışa ait para iadesi kaydı bulunamadı."
                });
            }

            if (refundPayment.IsCancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "Para iadesi kaydı zaten iptal edilmiş."
                });
            }

            string cancellationReason =
                dto.CancellationReason.Trim();

            if (string.IsNullOrWhiteSpace(
                    cancellationReason))
            {
                return BadRequest(new
                {
                    Message =
                        "Para iadesi iptal nedeni zorunludur."
                });
            }

            refundPayment.IsCancelled = true;
            refundPayment.CancelledAt = DateTime.UtcNow;
            refundPayment.CancellationReason =
                cancellationReason;

            sale.CustomerRefundedAmount =
                sale.SaleRefundPayments
                    .Where(payment =>
                        !payment.IsCancelled)
                    .Sum(payment =>
                        payment.Amount);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Müşteriye para iadesi kaydı başarıyla iptal edildi.",

                RefundPayment = new
                {
                    refundPayment.Id,
                    refundPayment.SaleId,
                    refundPayment.Amount,
                    refundPayment.PaymentMethod,
                    refundPayment.IsCancelled,
                    refundPayment.CancelledAt,
                    refundPayment.CancellationReason
                },

                SaleSummary = new
                {
                    sale.Id,
                    sale.InvoiceNumber,
                    sale.TotalAmount,
                    sale.ReturnedAmount,
                    sale.NetAmount,
                    sale.PaidAmount,
                    sale.CustomerRefundedAmount,
                    sale.RefundDueAmount
                }
            });
        }
    }
}