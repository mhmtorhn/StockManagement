using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/sales-reports")]
    [ApiController]
    public class SalesReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesReportsController(AppDbContext context)
        {
            _context = context;
        }

        // Müşteri bazlı net satış, borç ve para iadesi özetini getirir
        [HttpGet("customer-debts")]
        public async Task<IActionResult> GetCustomerDebts(
            string? searchText,
            bool onlyWithDebt = true)
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

            if (onlyWithDebt)
            {
                query = query.Where(customer =>
                    customer.Sales.Any(sale =>
                        sale.Status == SaleStatus.Approved &&
                        (sale.RemainingAmount > 0 ||
                         sale.RefundDueAmount > 0)));
            }
            else
            {
                query = query.Where(customer =>
                    customer.Sales.Any(sale =>
                        sale.Status == SaleStatus.Approved));
            }

            var customers = await query
                .Select(customer => new
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    customer.CustomerType,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.IsActive,

                    ApprovedSaleCount =
                        customer.Sales.Count(sale =>
                            sale.Status ==
                                SaleStatus.Approved),

                    UnpaidSaleCount =
                        customer.Sales.Count(sale =>
                            sale.Status ==
                                SaleStatus.Approved &&
                            sale.PaymentStatus ==
                                PaymentStatus.Unpaid),

                    PartiallyPaidSaleCount =
                        customer.Sales.Count(sale =>
                            sale.Status ==
                                SaleStatus.Approved &&
                            sale.PaymentStatus ==
                                PaymentStatus.PartiallyPaid),

                    PaidSaleCount =
                        customer.Sales.Count(sale =>
                            sale.Status ==
                                SaleStatus.Approved &&
                            sale.PaymentStatus ==
                                PaymentStatus.Paid),

                    GrossSaleAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.TotalAmount),

                    ReturnedAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.ReturnedAmount),

                    NetSaleAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.NetAmount),

                    TotalPaidAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.PaidAmount),

                    CustomerRefundedAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.CustomerRefundedAmount),

                    TotalRemainingAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.RemainingAmount),

                    RefundDueAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.RefundDueAmount),

                    GrossProfit =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.TotalProfit),

                    ReturnedProfitAmount =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.ReturnedProfitAmount),

                    NetProfit =
                        customer.Sales
                            .Where(sale =>
                                sale.Status ==
                                    SaleStatus.Approved)
                            .Sum(sale =>
                                sale.NetProfit)
                })
                .ToListAsync();

            var customerResults = customers
                .Select(customer => new
                {
                    customer.CustomerId,
                    customer.CustomerName,
                    customer.CustomerType,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.IsActive,
                    customer.ApprovedSaleCount,
                    customer.UnpaidSaleCount,
                    customer.PartiallyPaidSaleCount,
                    customer.PaidSaleCount,
                    customer.GrossSaleAmount,
                    customer.ReturnedAmount,
                    customer.NetSaleAmount,
                    customer.TotalPaidAmount,
                    customer.CustomerRefundedAmount,

                    NetCashCollected =
                        customer.TotalPaidAmount -
                        customer.CustomerRefundedAmount,

                    customer.TotalRemainingAmount,
                    customer.RefundDueAmount,

                    NetBalance =
                        customer.TotalRemainingAmount -
                        customer.RefundDueAmount,

                    customer.GrossProfit,
                    customer.ReturnedProfitAmount,
                    customer.NetProfit
                })
                .OrderByDescending(customer =>
                    Math.Abs(customer.NetBalance))
                .ToList();

            var summary = new
            {
                CustomerCount =
                    customerResults.Count,

                TotalApprovedSaleCount =
                    customerResults.Sum(customer =>
                        customer.ApprovedSaleCount),

                GrossSaleAmount =
                    customerResults.Sum(customer =>
                        customer.GrossSaleAmount),

                ReturnedAmount =
                    customerResults.Sum(customer =>
                        customer.ReturnedAmount),

                NetSaleAmount =
                    customerResults.Sum(customer =>
                        customer.NetSaleAmount),

                TotalPaidAmount =
                    customerResults.Sum(customer =>
                        customer.TotalPaidAmount),

                CustomerRefundedAmount =
                    customerResults.Sum(customer =>
                        customer.CustomerRefundedAmount),

                NetCashCollected =
                    customerResults.Sum(customer =>
                        customer.NetCashCollected),

                TotalRemainingAmount =
                    customerResults.Sum(customer =>
                        customer.TotalRemainingAmount),

                RefundDueAmount =
                    customerResults.Sum(customer =>
                        customer.RefundDueAmount),

                NetReceivableBalance =
                    customerResults.Sum(customer =>
                        customer.NetBalance),

                GrossProfit =
                    customerResults.Sum(customer =>
                        customer.GrossProfit),

                ReturnedProfitAmount =
                    customerResults.Sum(customer =>
                        customer.ReturnedProfitAmount),

                NetProfit =
                    customerResults.Sum(customer =>
                        customer.NetProfit)
            };

            return Ok(new
            {
                Summary = summary,
                Customers = customerResults
            });
        }
    }
}