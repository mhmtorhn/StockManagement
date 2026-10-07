using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/sales-reports")]
    [ApiController]
    public class SalesDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesDashboardController(AppDbContext context)
        {
            _context = context;
        }

        // Net satış, tahsilat, iade ve kârlılık raporunu getirir
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetSalesDashboard(
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

            IQueryable<Sale> saleQuery =
                _context.Sales.AsNoTracking();

            if (startDate.HasValue)
            {
                saleQuery = saleQuery.Where(sale =>
                    sale.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                saleQuery = saleQuery.Where(sale =>
                    sale.SaleDate <= endDate.Value);
            }

            int totalSaleCount =
                await saleQuery.CountAsync();

            int draftSaleCount =
                await saleQuery.CountAsync(sale =>
                    sale.Status == SaleStatus.Draft);

            int approvedSaleCount =
                await saleQuery.CountAsync(sale =>
                    sale.Status == SaleStatus.Approved);

            int cancelledSaleCount =
                await saleQuery.CountAsync(sale =>
                    sale.Status == SaleStatus.Cancelled);

            IQueryable<Sale> approvedQuery =
                saleQuery.Where(sale =>
                    sale.Status == SaleStatus.Approved);

            decimal grossSaleAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.TotalAmount)
                    .SumAsync() ?? 0;

            decimal returnedAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.ReturnedAmount)
                    .SumAsync() ?? 0;

            decimal netSaleAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.NetAmount)
                    .SumAsync() ?? 0;

            decimal grossProfit =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.TotalProfit)
                    .SumAsync() ?? 0;

            decimal returnedProfitAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.ReturnedProfitAmount)
                    .SumAsync() ?? 0;

            decimal netProfit =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.NetProfit)
                    .SumAsync() ?? 0;

            decimal netCost =
                netSaleAmount - netProfit;

            decimal totalPaidAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.PaidAmount)
                    .SumAsync() ?? 0;

            decimal customerRefundedAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.CustomerRefundedAmount)
                    .SumAsync() ?? 0;

            decimal refundDueAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.RefundDueAmount)
                    .SumAsync() ?? 0;

            decimal totalRemainingAmount =
                await approvedQuery
                    .Select(sale =>
                        (decimal?)sale.RemainingAmount)
                    .SumAsync() ?? 0;

            decimal netCashCollected =
                totalPaidAmount -
                customerRefundedAmount;

            decimal collectionRate = netSaleAmount > 0
                ? Math.Round(
                    netCashCollected /
                    netSaleAmount * 100,
                    2)
                : 0;

            decimal profitMargin = netSaleAmount > 0
                ? Math.Round(
                    netProfit /
                    netSaleAmount * 100,
                    2)
                : 0;

            int unpaidSaleCount =
                await approvedQuery.CountAsync(sale =>
                    sale.PaymentStatus ==
                        PaymentStatus.Unpaid);

            int partiallyPaidSaleCount =
                await approvedQuery.CountAsync(sale =>
                    sale.PaymentStatus ==
                        PaymentStatus.PartiallyPaid);

            int paidSaleCount =
                await approvedQuery.CountAsync(sale =>
                    sale.PaymentStatus ==
                        PaymentStatus.Paid);

            int overdueSaleCount =
                await approvedQuery.CountAsync(sale =>
                    sale.RemainingAmount > 0 &&
                    sale.DueDate.HasValue &&
                    sale.DueDate.Value <
                        DateTime.UtcNow);

            decimal overdueAmount =
                await approvedQuery
                    .Where(sale =>
                        sale.RemainingAmount > 0 &&
                        sale.DueDate.HasValue &&
                        sale.DueDate.Value <
                            DateTime.UtcNow)
                    .Select(sale =>
                        (decimal?)sale.RemainingAmount)
                    .SumAsync() ?? 0;

            IQueryable<SaleReturn> approvedReturnQuery =
                _context.SaleReturns
                    .AsNoTracking()
                    .Where(saleReturn =>
                        saleReturn.Status ==
                            SaleReturnStatus.Approved);

            if (startDate.HasValue)
            {
                approvedReturnQuery =
                    approvedReturnQuery.Where(saleReturn =>
                        saleReturn.Sale.SaleDate >=
                            startDate.Value);
            }

            if (endDate.HasValue)
            {
                approvedReturnQuery =
                    approvedReturnQuery.Where(saleReturn =>
                        saleReturn.Sale.SaleDate <=
                            endDate.Value);
            }

            int approvedReturnCount =
                await approvedReturnQuery.CountAsync();

            var paymentMethodDistribution =
                await approvedQuery
                    .GroupBy(sale =>
                        sale.PaymentMethod)
                    .Select(group => new
                    {
                        PaymentMethod = group.Key,
                        SaleCount = group.Count(),

                        GrossSaleAmount =
                            group.Sum(sale =>
                                sale.TotalAmount),

                        ReturnedAmount =
                            group.Sum(sale =>
                                sale.ReturnedAmount),

                        NetSaleAmount =
                            group.Sum(sale =>
                                sale.NetAmount),

                        PaidAmount =
                            group.Sum(sale =>
                                sale.PaidAmount),

                        CustomerRefundedAmount =
                            group.Sum(sale =>
                                sale.CustomerRefundedAmount),

                        RemainingAmount =
                            group.Sum(sale =>
                                sale.RemainingAmount),

                        RefundDueAmount =
                            group.Sum(sale =>
                                sale.RefundDueAmount),

                        NetProfit =
                            group.Sum(sale =>
                                sale.NetProfit)
                    })
                    .OrderByDescending(item =>
                        item.NetSaleAmount)
                    .ToListAsync();

            var monthlySales =
                await approvedQuery
                    .GroupBy(sale => new
                    {
                        sale.SaleDate.Year,
                        sale.SaleDate.Month
                    })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,

                        SaleCount = group.Count(),

                        GrossSaleAmount =
                            group.Sum(sale =>
                                sale.TotalAmount),

                        ReturnedAmount =
                            group.Sum(sale =>
                                sale.ReturnedAmount),

                        NetSaleAmount =
                            group.Sum(sale =>
                                sale.NetAmount),

                        GrossProfit =
                            group.Sum(sale =>
                                sale.TotalProfit),

                        ReturnedProfitAmount =
                            group.Sum(sale =>
                                sale.ReturnedProfitAmount),

                        NetProfit =
                            group.Sum(sale =>
                                sale.NetProfit),

                        PaidAmount =
                            group.Sum(sale =>
                                sale.PaidAmount),

                        CustomerRefundedAmount =
                            group.Sum(sale =>
                                sale.CustomerRefundedAmount),

                        RemainingAmount =
                            group.Sum(sale =>
                                sale.RemainingAmount),

                        RefundDueAmount =
                            group.Sum(sale =>
                                sale.RefundDueAmount)
                    })
                    .OrderBy(item => item.Year)
                    .ThenBy(item => item.Month)
                    .ToListAsync();

            IQueryable<SaleItem> saleItemQuery =
                _context.SaleItems
                    .AsNoTracking()
                    .Where(item =>
                        item.Sale.Status ==
                            SaleStatus.Approved);

            if (startDate.HasValue)
            {
                saleItemQuery = saleItemQuery.Where(item =>
                    item.Sale.SaleDate >=
                        startDate.Value);
            }

            if (endDate.HasValue)
            {
                saleItemQuery = saleItemQuery.Where(item =>
                    item.Sale.SaleDate <=
                        endDate.Value);
            }

            var soldProducts =
                await saleItemQuery
                    .GroupBy(item => new
                    {
                        item.ProductId,
                        ProductName = item.Product.Name
                    })
                    .Select(group => new
                    {
                        group.Key.ProductId,
                        group.Key.ProductName,

                        SoldQuantity =
                            group.Sum(item =>
                                item.Quantity),

                        GrossSaleAmount =
                            group.Sum(item =>
                                item.TotalPrice),

                        GrossCostAmount =
                            group.Sum(item =>
                                item.CostPrice *
                                item.Quantity),

                        GrossProfit =
                            group.Sum(item =>
                                item.ProfitAmount)
                    })
                    .ToListAsync();

            IQueryable<SaleReturnItem> returnItemQuery =
                _context.SaleReturnItems
                    .AsNoTracking()
                    .Where(item =>
                        item.SaleReturn.Status ==
                            SaleReturnStatus.Approved);

            if (startDate.HasValue)
            {
                returnItemQuery =
                    returnItemQuery.Where(item =>
                        item.SaleReturn.Sale.SaleDate >=
                            startDate.Value);
            }

            if (endDate.HasValue)
            {
                returnItemQuery =
                    returnItemQuery.Where(item =>
                        item.SaleReturn.Sale.SaleDate <=
                            endDate.Value);
            }

            var returnedProducts =
                await returnItemQuery
                    .GroupBy(item => item.ProductId)
                    .Select(group => new
                    {
                        ProductId = group.Key,

                        ReturnedQuantity =
                            group.Sum(item =>
                                item.Quantity),

                        ReturnedAmount =
                            group.Sum(item =>
                                item.RefundAmount),

                        ReturnedCostAmount =
                            group.Sum(item =>
                                item.CostPrice *
                                item.Quantity),

                        ReturnedProfitAmount =
                            group.Sum(item =>
                                item.ProfitAdjustment)
                    })
                    .ToListAsync();

            var returnedProductLookup =
                returnedProducts.ToDictionary(
                    item => item.ProductId);

            var topSellingProducts = soldProducts
                .Select(product =>
                {
                    returnedProductLookup.TryGetValue(
                        product.ProductId,
                        out var returned);

                    int returnedQuantity =
                        returned?.ReturnedQuantity ?? 0;

                    decimal returnedProductAmount =
                        returned?.ReturnedAmount ?? 0;

                    decimal returnedCostAmount =
                        returned?.ReturnedCostAmount ?? 0;

                    decimal returnedProductProfit =
                        returned?.ReturnedProfitAmount ?? 0;

                    return new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.SoldQuantity,
                        ReturnedQuantity =
                            returnedQuantity,
                        NetSoldQuantity =
                            product.SoldQuantity -
                            returnedQuantity,
                        product.GrossSaleAmount,
                        ReturnedAmount =
                            returnedProductAmount,
                        NetSaleAmount =
                            product.GrossSaleAmount -
                            returnedProductAmount,
                        product.GrossCostAmount,
                        ReturnedCostAmount =
                            returnedCostAmount,
                        NetCostAmount =
                            product.GrossCostAmount -
                            returnedCostAmount,
                        product.GrossProfit,
                        ReturnedProfitAmount =
                            returnedProductProfit,
                        NetProfit =
                            product.GrossProfit -
                            returnedProductProfit
                    };
                })
                .OrderByDescending(product =>
                    product.NetSoldQuantity)
                .ThenByDescending(product =>
                    product.NetSaleAmount)
                .Take(10)
                .ToList();

            var recentSales =
                await saleQuery
                    .OrderByDescending(sale =>
                        sale.SaleDate)
                    .ThenByDescending(sale =>
                        sale.Id)
                    .Take(10)
                    .Select(sale => new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        sale.CustomerId,
                        CustomerName =
                            sale.Customer.Name,
                        sale.SaleDate,
                        sale.DueDate,
                        sale.TotalAmount,
                        sale.ReturnedAmount,
                        sale.NetAmount,
                        sale.PaidAmount,
                        sale.CustomerRefundedAmount,
                        sale.RemainingAmount,
                        sale.RefundDueAmount,
                        sale.TotalProfit,
                        sale.ReturnedProfitAmount,
                        sale.NetProfit,
                        sale.PaymentMethod,
                        sale.PaymentStatus,
                        sale.Status
                    })
                    .ToListAsync();

            return Ok(new
            {
                DateFilter = new
                {
                    StartDate = startDate,
                    EndDate = endDate
                },

                SaleSummary = new
                {
                    TotalSaleCount = totalSaleCount,
                    DraftSaleCount = draftSaleCount,
                    ApprovedSaleCount =
                        approvedSaleCount,
                    CancelledSaleCount =
                        cancelledSaleCount,
                    ApprovedReturnCount =
                        approvedReturnCount
                },

                FinancialSummary = new
                {
                    GrossSaleAmount =
                        grossSaleAmount,
                    ReturnedAmount =
                        returnedAmount,
                    NetSaleAmount =
                        netSaleAmount,
                    NetCost = netCost,
                    GrossProfit =
                        grossProfit,
                    ReturnedProfitAmount =
                        returnedProfitAmount,
                    NetProfit = netProfit,
                    ProfitMargin = profitMargin,
                    TotalPaidAmount =
                        totalPaidAmount,
                    CustomerRefundedAmount =
                        customerRefundedAmount,
                    NetCashCollected =
                        netCashCollected,
                    TotalRemainingAmount =
                        totalRemainingAmount,
                    RefundDueAmount =
                        refundDueAmount,
                    CollectionRate =
                        collectionRate
                },

                PaymentSummary = new
                {
                    UnpaidSaleCount =
                        unpaidSaleCount,
                    PartiallyPaidSaleCount =
                        partiallyPaidSaleCount,
                    PaidSaleCount =
                        paidSaleCount,
                    OverdueSaleCount =
                        overdueSaleCount,
                    OverdueAmount =
                        overdueAmount
                },

                PaymentMethodDistribution =
                    paymentMethodDistribution,

                MonthlySales = monthlySales,

                TopSellingProducts =
                    topSellingProducts,

                RecentSales = recentSales
            });
        }
    }
}