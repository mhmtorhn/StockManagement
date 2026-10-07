using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/purchase-reports")]
    [ApiController]
    public class PurchaseReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseReportsController(AppDbContext context)
        {
            _context = context;
        }

        // Satın alma yönetiminin genel dashboard raporunu getirir
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetPurchaseDashboard(
            DateTime? startDate,
            DateTime? endDate)
        {
            if (startDate.HasValue &&
                endDate.HasValue &&
                startDate.Value > endDate.Value)
            {
                return BadRequest(new
                {
                    Message =
                        "Başlangıç tarihi bitiş tarihinden sonra olamaz."
                });
            }

            IQueryable<Purchase> purchaseQuery =
                _context.Purchases.AsNoTracking();

            if (startDate.HasValue)
            {
                purchaseQuery = purchaseQuery.Where(purchase =>
                    purchase.PurchaseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                purchaseQuery = purchaseQuery.Where(purchase =>
                    purchase.PurchaseDate <= endDate.Value);
            }

            int totalPurchaseCount =
                await purchaseQuery.CountAsync();

            int draftPurchaseCount =
                await purchaseQuery.CountAsync(purchase =>
                    purchase.Status == PurchaseStatus.Draft);

            int approvedPurchaseCount =
                await purchaseQuery.CountAsync(purchase =>
                    purchase.Status == PurchaseStatus.Approved);

            int cancelledPurchaseCount =
                await purchaseQuery.CountAsync(purchase =>
                    purchase.Status == PurchaseStatus.Cancelled);

            IQueryable<Purchase> approvedQuery =
                purchaseQuery.Where(purchase =>
                    purchase.Status == PurchaseStatus.Approved);

            decimal totalPurchaseAmount =
                await approvedQuery
                    .Select(purchase =>
                        (decimal?)purchase.TotalAmount)
                    .SumAsync() ?? 0;

            decimal totalPaidAmount =
                await approvedQuery
                    .Select(purchase =>
                        (decimal?)purchase.PaidAmount)
                    .SumAsync() ?? 0;

            decimal totalRemainingAmount =
                await approvedQuery
                    .Select(purchase =>
                        (decimal?)purchase.RemainingAmount)
                    .SumAsync() ?? 0;

            int unpaidPurchaseCount =
                await approvedQuery.CountAsync(purchase =>
                    purchase.PaymentStatus ==
                        PaymentStatus.Unpaid);

            int partiallyPaidPurchaseCount =
                await approvedQuery.CountAsync(purchase =>
                    purchase.PaymentStatus ==
                        PaymentStatus.PartiallyPaid);

            int paidPurchaseCount =
                await approvedQuery.CountAsync(purchase =>
                    purchase.PaymentStatus ==
                        PaymentStatus.Paid);

            int activePaymentCount =
                await _context.PurchasePayments
                    .AsNoTracking()
                    .CountAsync(payment =>
                        !payment.IsCancelled &&
                        payment.Purchase.Status ==
                            PurchaseStatus.Approved &&
                        (!startDate.HasValue ||
                            payment.PaymentDate >=
                                startDate.Value) &&
                        (!endDate.HasValue ||
                            payment.PaymentDate <=
                                endDate.Value));

            int cancelledPaymentCount =
                await _context.PurchasePayments
                    .AsNoTracking()
                    .CountAsync(payment =>
                        payment.IsCancelled &&
                        payment.Purchase.Status ==
                            PurchaseStatus.Approved &&
                        (!startDate.HasValue ||
                            payment.PaymentDate >=
                                startDate.Value) &&
                        (!endDate.HasValue ||
                            payment.PaymentDate <=
                                endDate.Value));

            var paymentMethodDistribution =
                await approvedQuery
                    .GroupBy(purchase =>
                        purchase.PaymentMethod)
                    .Select(group => new
                    {
                        PaymentMethod = group.Key,
                        PurchaseCount = group.Count(),
                        TotalAmount = group.Sum(purchase =>
                            purchase.TotalAmount),
                        PaidAmount = group.Sum(purchase =>
                            purchase.PaidAmount),
                        RemainingAmount = group.Sum(purchase =>
                            purchase.RemainingAmount)
                    })
                    .OrderByDescending(item =>
                        item.TotalAmount)
                    .ToListAsync();

            var monthlyPurchaseTotals =
                await approvedQuery
                    .GroupBy(purchase => new
                    {
                        purchase.PurchaseDate.Year,
                        purchase.PurchaseDate.Month
                    })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        PurchaseCount = group.Count(),
                        TotalAmount = group.Sum(purchase =>
                            purchase.TotalAmount),
                        PaidAmount = group.Sum(purchase =>
                            purchase.PaidAmount),
                        RemainingAmount = group.Sum(purchase =>
                            purchase.RemainingAmount)
                    })
                    .OrderBy(item => item.Year)
                    .ThenBy(item => item.Month)
                    .ToListAsync();

            var recentPurchases =
                await purchaseQuery
                    .OrderByDescending(purchase =>
                        purchase.PurchaseDate)
                    .ThenByDescending(purchase =>
                        purchase.Id)
                    .Take(10)
                    .Select(purchase => new
                    {
                        purchase.Id,
                        purchase.InvoiceNumber,
                        purchase.SupplierId,
                        SupplierName = purchase.Supplier.Name,
                        purchase.PurchaseDate,
                        purchase.TotalAmount,
                        purchase.PaidAmount,
                        purchase.RemainingAmount,
                        purchase.PaymentMethod,
                        purchase.PaymentStatus,
                        purchase.Status
                    })
                    .ToListAsync();

            decimal paymentRate = totalPurchaseAmount > 0
                ? Math.Round(
                    totalPaidAmount / totalPurchaseAmount * 100,
                    2)
                : 0;

            return Ok(new
            {
                DateFilter = new
                {
                    StartDate = startDate,
                    EndDate = endDate
                },

                PurchaseSummary = new
                {
                    TotalPurchaseCount = totalPurchaseCount,
                    DraftPurchaseCount = draftPurchaseCount,
                    ApprovedPurchaseCount =
                        approvedPurchaseCount,
                    CancelledPurchaseCount =
                        cancelledPurchaseCount
                },

                FinancialSummary = new
                {
                    TotalPurchaseAmount =
                        totalPurchaseAmount,
                    TotalPaidAmount = totalPaidAmount,
                    TotalRemainingAmount =
                        totalRemainingAmount,
                    PaymentRate = paymentRate
                },

                PaymentSummary = new
                {
                    UnpaidPurchaseCount =
                        unpaidPurchaseCount,
                    PartiallyPaidPurchaseCount =
                        partiallyPaidPurchaseCount,
                    PaidPurchaseCount =
                        paidPurchaseCount,
                    ActivePaymentCount =
                        activePaymentCount,
                    CancelledPaymentCount =
                        cancelledPaymentCount
                },

                PaymentMethodDistribution =
                    paymentMethodDistribution,

                MonthlyPurchaseTotals =
                    monthlyPurchaseTotals,

                RecentPurchases = recentPurchases
            });
        }

        // Tedarikçi bazlı satın alma ve borç özetini getirir
        [HttpGet("supplier-debts")]
        public async Task<IActionResult> GetSupplierDebts(
            string? searchText,
            bool onlyWithDebt = true)
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
                    (supplier.ContactPerson ?? string.Empty)
                        .ToLower()
                        .Contains(normalizedSearchText) ||
                    (supplier.PhoneNumber ?? string.Empty)
                        .ToLower()
                        .Contains(normalizedSearchText));
            }

            if (onlyWithDebt)
            {
                query = query.Where(supplier =>
                    supplier.Purchases.Any(purchase =>
                        purchase.Status ==
                            PurchaseStatus.Approved &&
                        purchase.RemainingAmount > 0));
            }
            else
            {
                query = query.Where(supplier =>
                    supplier.Purchases.Any(purchase =>
                        purchase.Status ==
                            PurchaseStatus.Approved));
            }

            var suppliers = await query
                .OrderByDescending(supplier =>
                    supplier.Purchases
                        .Where(purchase =>
                            purchase.Status ==
                                PurchaseStatus.Approved)
                        .Sum(purchase =>
                            purchase.RemainingAmount))
                .Select(supplier => new
                {
                    SupplierId = supplier.Id,
                    SupplierName = supplier.Name,
                    supplier.ContactPerson,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.IsActive,

                    ApprovedPurchaseCount =
                        supplier.Purchases.Count(purchase =>
                            purchase.Status ==
                                PurchaseStatus.Approved),

                    UnpaidPurchaseCount =
                        supplier.Purchases.Count(purchase =>
                            purchase.Status ==
                                PurchaseStatus.Approved &&
                            purchase.PaymentStatus ==
                                PaymentStatus.Unpaid),

                    PartiallyPaidPurchaseCount =
                        supplier.Purchases.Count(purchase =>
                            purchase.Status ==
                                PurchaseStatus.Approved &&
                            purchase.PaymentStatus ==
                                PaymentStatus.PartiallyPaid),

                    PaidPurchaseCount =
                        supplier.Purchases.Count(purchase =>
                            purchase.Status ==
                                PurchaseStatus.Approved &&
                            purchase.PaymentStatus ==
                                PaymentStatus.Paid),

                    TotalPurchaseAmount =
                        supplier.Purchases
                            .Where(purchase =>
                                purchase.Status ==
                                    PurchaseStatus.Approved)
                            .Sum(purchase =>
                                purchase.TotalAmount),

                    TotalPaidAmount =
                        supplier.Purchases
                            .Where(purchase =>
                                purchase.Status ==
                                    PurchaseStatus.Approved)
                            .Sum(purchase =>
                                purchase.PaidAmount),

                    TotalRemainingAmount =
                        supplier.Purchases
                            .Where(purchase =>
                                purchase.Status ==
                                    PurchaseStatus.Approved)
                            .Sum(purchase =>
                                purchase.RemainingAmount)
                })
                .ToListAsync();

            var reportSummary = new
            {
                SupplierCount = suppliers.Count,

                TotalApprovedPurchaseCount =
                    suppliers.Sum(supplier =>
                        supplier.ApprovedPurchaseCount),

                TotalPurchaseAmount =
                    suppliers.Sum(supplier =>
                        supplier.TotalPurchaseAmount),

                TotalPaidAmount =
                    suppliers.Sum(supplier =>
                        supplier.TotalPaidAmount),

                TotalRemainingAmount =
                    suppliers.Sum(supplier =>
                        supplier.TotalRemainingAmount)
            };

            return Ok(new
            {
                Summary = reportSummary,
                Suppliers = suppliers
            });
        }

        // Tek tedarikçinin satın alma ve ödeme ekstresini getirir
        [HttpGet("suppliers/{supplierId:int}/account-statement")]
        public async Task<IActionResult> GetSupplierAccountStatement(
            int supplierId,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (startDate.HasValue &&
                endDate.HasValue &&
                startDate.Value > endDate.Value)
            {
                return BadRequest(new
                {
                    Message =
                        "Başlangıç tarihi bitiş tarihinden sonra olamaz."
                });
            }

            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(supplier =>
                    supplier.Id == supplierId)
                .Select(supplier => new
                {
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.PhoneNumber,
                    supplier.Email,
                    supplier.Address,
                    supplier.IsActive
                })
                .FirstOrDefaultAsync();

            if (supplier is null)
            {
                return NotFound(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            IQueryable<Purchase> purchaseQuery =
                _context.Purchases
                    .AsNoTracking()
                    .Where(purchase =>
                        purchase.SupplierId == supplierId &&
                        purchase.Status ==
                            PurchaseStatus.Approved);

            if (startDate.HasValue)
            {
                purchaseQuery = purchaseQuery.Where(purchase =>
                    purchase.PurchaseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                purchaseQuery = purchaseQuery.Where(purchase =>
                    purchase.PurchaseDate <= endDate.Value);
            }

            var purchases = await purchaseQuery
                .OrderByDescending(purchase =>
                    purchase.PurchaseDate)
                .ThenByDescending(purchase =>
                    purchase.Id)
                .Select(purchase => new
                {
                    purchase.Id,
                    purchase.InvoiceNumber,
                    purchase.PurchaseDate,
                    purchase.ApprovedAt,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentMethod,
                    purchase.PaymentStatus,
                    purchase.Description,

                    ItemCount = purchase.PurchaseItems.Count,

                    ActivePaymentCount =
                        purchase.PurchasePayments.Count(payment =>
                            !payment.IsCancelled),

                    CancelledPaymentCount =
                        purchase.PurchasePayments.Count(payment =>
                            payment.IsCancelled),

                    Payments = purchase.PurchasePayments
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
                .ToListAsync();

            var summary = new
            {
                ApprovedPurchaseCount = purchases.Count,

                UnpaidPurchaseCount =
                    purchases.Count(purchase =>
                        purchase.PaymentStatus ==
                            PaymentStatus.Unpaid),

                PartiallyPaidPurchaseCount =
                    purchases.Count(purchase =>
                        purchase.PaymentStatus ==
                            PaymentStatus.PartiallyPaid),

                PaidPurchaseCount =
                    purchases.Count(purchase =>
                        purchase.PaymentStatus ==
                            PaymentStatus.Paid),

                TotalPurchaseAmount =
                    purchases.Sum(purchase =>
                        purchase.TotalAmount),

                TotalPaidAmount =
                    purchases.Sum(purchase =>
                        purchase.PaidAmount),

                TotalRemainingAmount =
                    purchases.Sum(purchase =>
                        purchase.RemainingAmount),

                ActivePaymentCount =
                    purchases.Sum(purchase =>
                        purchase.ActivePaymentCount),

                CancelledPaymentCount =
                    purchases.Sum(purchase =>
                        purchase.CancelledPaymentCount)
            };

            return Ok(new
            {
                Supplier = supplier,

                DateFilter = new
                {
                    StartDate = startDate,
                    EndDate = endDate
                },

                Summary = summary,
                Purchases = purchases
            });
        }
    }
}