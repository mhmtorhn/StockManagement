using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/purchase-reports")]
    [ApiController]
    public class OverduePurchaseReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OverduePurchaseReportsController(
            AppDbContext context)
        {
            _context = context;
        }

        // Gecikmiş ve borcu devam eden satın almaları raporlar
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverduePurchases(
            int? supplierId)
        {
            DateTime now = DateTime.UtcNow;

            IQueryable<Purchase> query = _context.Purchases
                .AsNoTracking()
                .Where(purchase =>
                    purchase.Status == PurchaseStatus.Approved &&
                    purchase.RemainingAmount > 0 &&
                    purchase.DueDate.HasValue &&
                    purchase.DueDate.Value < now);

            if (supplierId.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.SupplierId == supplierId.Value);
            }

            var overduePurchases = await query
                .OrderBy(purchase => purchase.DueDate)
                .ThenBy(purchase => purchase.Id)
                .Select(purchase => new
                {
                    purchase.Id,
                    purchase.SupplierId,
                    SupplierName = purchase.Supplier.Name,
                    purchase.InvoiceNumber,
                    purchase.PurchaseDate,
                    purchase.DueDate,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentMethod,
                    purchase.PaymentStatus,

                    OverdueDays =
                        (now.Date -
                            purchase.DueDate!.Value.Date).Days,

                    ActivePaymentCount =
                        purchase.PurchasePayments.Count(payment =>
                            !payment.IsCancelled),

                    LastPaymentDate =
                        purchase.PurchasePayments
                            .Where(payment =>
                                !payment.IsCancelled)
                            .Select(payment =>
                                (DateTime?)payment.PaymentDate)
                            .Max()
                })
                .ToListAsync();

            var supplierSummaries = overduePurchases
                .GroupBy(purchase => new
                {
                    purchase.SupplierId,
                    purchase.SupplierName
                })
                .Select(group => new
                {
                    group.Key.SupplierId,
                    group.Key.SupplierName,

                    OverduePurchaseCount = group.Count(),

                    TotalPurchaseAmount =
                        group.Sum(purchase =>
                            purchase.TotalAmount),

                    TotalPaidAmount =
                        group.Sum(purchase =>
                            purchase.PaidAmount),

                    TotalOverdueAmount =
                        group.Sum(purchase =>
                            purchase.RemainingAmount),

                    MaximumOverdueDays =
                        group.Max(purchase =>
                            purchase.OverdueDays),

                    OldestDueDate =
                        group.Min(purchase =>
                            purchase.DueDate)
                })
                .OrderByDescending(summary =>
                    summary.TotalOverdueAmount)
                .ToList();

            var summary = new
            {
                OverduePurchaseCount =
                    overduePurchases.Count,

                SupplierCount =
                    supplierSummaries.Count,

                TotalPurchaseAmount =
                    overduePurchases.Sum(purchase =>
                        purchase.TotalAmount),

                TotalPaidAmount =
                    overduePurchases.Sum(purchase =>
                        purchase.PaidAmount),

                TotalOverdueAmount =
                    overduePurchases.Sum(purchase =>
                        purchase.RemainingAmount),

                MaximumOverdueDays =
                    overduePurchases.Count > 0
                        ? overduePurchases.Max(purchase =>
                            purchase.OverdueDays)
                        : 0
            };

            return Ok(new
            {
                ReportDate = now,

                Filter = new
                {
                    SupplierId = supplierId
                },

                Summary = summary,

                SupplierSummaries =
                    supplierSummaries,

                OverduePurchases =
                    overduePurchases
            });
        }
    }
}