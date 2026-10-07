using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/sales-reports")]
    [ApiController]
    public class OverdueSalesReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OverdueSalesReportsController(
            AppDbContext context)
        {
            _context = context;
        }

        // Vadesi geçmiş ve borcu devam eden satışları getirir
        [HttpGet("overdue-receivables")]
        public async Task<IActionResult> GetOverdueReceivables(
            int? customerId)
        {
            DateTime now = DateTime.UtcNow;

            IQueryable<Sale> query = _context.Sales
                .AsNoTracking()
                .Where(sale =>
                    sale.Status == SaleStatus.Approved &&
                    sale.RemainingAmount > 0 &&
                    sale.DueDate.HasValue &&
                    sale.DueDate.Value < now);

            if (customerId.HasValue)
            {
                query = query.Where(sale =>
                    sale.CustomerId == customerId.Value);
            }

            var overdueSales = await query
                .OrderBy(sale => sale.DueDate)
                .ThenBy(sale => sale.Id)
                .Select(sale => new
                {
                    sale.Id,
                    sale.CustomerId,
                    CustomerName = sale.Customer.Name,
                    sale.InvoiceNumber,
                    sale.SaleDate,
                    sale.DueDate,
                    sale.TotalAmount,
                    sale.PaidAmount,
                    sale.RemainingAmount,
                    sale.TotalProfit,
                    sale.PaymentMethod,
                    sale.PaymentStatus,

                    OverdueDays =
                        (now.Date -
                            sale.DueDate!.Value.Date).Days,

                    ActivePaymentCount =
                        sale.SalePayments.Count(payment =>
                            !payment.IsCancelled),

                    LastPaymentDate =
                        sale.SalePayments
                            .Where(payment =>
                                !payment.IsCancelled)
                            .Select(payment =>
                                (DateTime?)payment.PaymentDate)
                            .Max()
                })
                .ToListAsync();

            var customerSummaries = overdueSales
                .GroupBy(sale => new
                {
                    sale.CustomerId,
                    sale.CustomerName
                })
                .Select(group => new
                {
                    group.Key.CustomerId,
                    group.Key.CustomerName,

                    OverdueSaleCount = group.Count(),

                    TotalSaleAmount =
                        group.Sum(sale =>
                            sale.TotalAmount),

                    TotalPaidAmount =
                        group.Sum(sale =>
                            sale.PaidAmount),

                    TotalOverdueAmount =
                        group.Sum(sale =>
                            sale.RemainingAmount),

                    TotalProfit =
                        group.Sum(sale =>
                            sale.TotalProfit),

                    MaximumOverdueDays =
                        group.Max(sale =>
                            sale.OverdueDays),

                    OldestDueDate =
                        group.Min(sale =>
                            sale.DueDate)
                })
                .OrderByDescending(summary =>
                    summary.TotalOverdueAmount)
                .ToList();

            var summary = new
            {
                OverdueSaleCount =
                    overdueSales.Count,

                CustomerCount =
                    customerSummaries.Count,

                TotalSaleAmount =
                    overdueSales.Sum(sale =>
                        sale.TotalAmount),

                TotalPaidAmount =
                    overdueSales.Sum(sale =>
                        sale.PaidAmount),

                TotalOverdueAmount =
                    overdueSales.Sum(sale =>
                        sale.RemainingAmount),

                TotalProfit =
                    overdueSales.Sum(sale =>
                        sale.TotalProfit),

                MaximumOverdueDays =
                    overdueSales.Count > 0
                        ? overdueSales.Max(sale =>
                            sale.OverdueDays)
                        : 0
            };

            return Ok(new
            {
                ReportDate = now,

                Filter = new
                {
                    CustomerId = customerId
                },

                Summary = summary,

                CustomerSummaries =
                    customerSummaries,

                OverdueSales =
                    overdueSales
            });
        }
    }
}