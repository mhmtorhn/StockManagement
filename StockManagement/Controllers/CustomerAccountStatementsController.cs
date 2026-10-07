using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/sales-reports")]
    [ApiController]
    public class CustomerAccountStatementsController
        : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerAccountStatementsController(
            AppDbContext context)
        {
            _context = context;
        }

        // Müşterinin net satış, tahsilat ve iade ekstresini getirir
        [HttpGet("customers/{customerId:int}/account-statement")]
        public async Task<IActionResult> GetCustomerAccountStatement(
            int customerId,
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

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(customer =>
                    customer.Id == customerId)
                .Select(customer => new
                {
                    customer.Id,
                    customer.CustomerType,
                    customer.Name,
                    customer.ContactPerson,
                    customer.PhoneNumber,
                    customer.Email,
                    customer.Address,
                    customer.TaxNumber,
                    customer.IsActive
                })
                .FirstOrDefaultAsync();

            if (customer is null)
            {
                return NotFound(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            IQueryable<Sale> saleQuery =
                _context.Sales
                    .AsNoTracking()
                    .Where(sale =>
                        sale.CustomerId == customerId &&
                        sale.Status ==
                            SaleStatus.Approved);

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

            var sales = await saleQuery
                .OrderByDescending(sale =>
                    sale.SaleDate)
                .ThenByDescending(sale =>
                    sale.Id)
                .Select(sale => new
                {
                    sale.Id,
                    sale.InvoiceNumber,
                    sale.SaleDate,
                    sale.DueDate,
                    sale.ApprovedAt,

                    GrossSaleAmount =
                        sale.TotalAmount,

                    sale.ReturnedAmount,
                    sale.NetAmount,
                    sale.PaidAmount,
                    sale.CustomerRefundedAmount,

                    NetCashCollected =
                        sale.PaidAmount -
                        sale.CustomerRefundedAmount,

                    sale.RemainingAmount,
                    sale.RefundDueAmount,

                    NetBalance =
                        sale.RemainingAmount -
                        sale.RefundDueAmount,

                    GrossProfit =
                        sale.TotalProfit,

                    sale.ReturnedProfitAmount,
                    sale.NetProfit,
                    sale.PaymentMethod,
                    sale.PaymentStatus,
                    sale.Description,

                    ItemCount =
                        sale.SaleItems.Count,

                    ActivePaymentCount =
                        sale.SalePayments.Count(payment =>
                            !payment.IsCancelled),

                    CancelledPaymentCount =
                        sale.SalePayments.Count(payment =>
                            payment.IsCancelled),

                    ApprovedReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                                SaleReturnStatus.Approved),

                    DraftReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                                SaleReturnStatus.Draft),

                    ActiveRefundPaymentCount =
                        sale.SaleRefundPayments.Count(payment =>
                            !payment.IsCancelled),

                    CancelledRefundPaymentCount =
                        sale.SaleRefundPayments.Count(payment =>
                            payment.IsCancelled),

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
                        .ToList(),

                    Returns = sale.SaleReturns
                        .OrderByDescending(saleReturn =>
                            saleReturn.ReturnDate)
                        .ThenByDescending(saleReturn =>
                            saleReturn.Id)
                        .Select(saleReturn => new
                        {
                            saleReturn.Id,
                            saleReturn.ReturnNumber,
                            saleReturn.ReturnDate,
                            saleReturn.TotalRefundAmount,
                            saleReturn.TotalCostAmount,
                            saleReturn.TotalProfitAdjustment,
                            saleReturn.Reason,
                            saleReturn.Status,
                            saleReturn.CreatedAt,
                            saleReturn.ApprovedAt,
                            saleReturn.CancelledAt,
                            saleReturn.CancellationReason,

                            Items =
                                saleReturn.SaleReturnItems
                                    .OrderBy(item =>
                                        item.Id)
                                    .Select(item => new
                                    {
                                        item.Id,
                                        item.SaleItemId,
                                        item.ProductId,
                                        ProductName =
                                            item.Product.Name,
                                        item.Quantity,
                                        item.UnitPrice,
                                        item.CostPrice,
                                        item.RefundAmount,
                                        item.ProfitAdjustment
                                    })
                                    .ToList()
                        })
                        .ToList(),

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
                .ToListAsync();

            var summary = new
            {
                ApprovedSaleCount = sales.Count,

                UnpaidSaleCount =
                    sales.Count(sale =>
                        sale.PaymentStatus ==
                            PaymentStatus.Unpaid),

                PartiallyPaidSaleCount =
                    sales.Count(sale =>
                        sale.PaymentStatus ==
                            PaymentStatus.PartiallyPaid),

                PaidSaleCount =
                    sales.Count(sale =>
                        sale.PaymentStatus ==
                            PaymentStatus.Paid),

                GrossSaleAmount =
                    sales.Sum(sale =>
                        sale.GrossSaleAmount),

                ReturnedAmount =
                    sales.Sum(sale =>
                        sale.ReturnedAmount),

                NetSaleAmount =
                    sales.Sum(sale =>
                        sale.NetAmount),

                TotalPaidAmount =
                    sales.Sum(sale =>
                        sale.PaidAmount),

                CustomerRefundedAmount =
                    sales.Sum(sale =>
                        sale.CustomerRefundedAmount),

                NetCashCollected =
                    sales.Sum(sale =>
                        sale.NetCashCollected),

                TotalRemainingAmount =
                    sales.Sum(sale =>
                        sale.RemainingAmount),

                RefundDueAmount =
                    sales.Sum(sale =>
                        sale.RefundDueAmount),

                NetBalance =
                    sales.Sum(sale =>
                        sale.NetBalance),

                GrossProfit =
                    sales.Sum(sale =>
                        sale.GrossProfit),

                ReturnedProfitAmount =
                    sales.Sum(sale =>
                        sale.ReturnedProfitAmount),

                NetProfit =
                    sales.Sum(sale =>
                        sale.NetProfit),

                ApprovedReturnCount =
                    sales.Sum(sale =>
                        sale.ApprovedReturnCount),

                DraftReturnCount =
                    sales.Sum(sale =>
                        sale.DraftReturnCount),

                ActivePaymentCount =
                    sales.Sum(sale =>
                        sale.ActivePaymentCount),

                CancelledPaymentCount =
                    sales.Sum(sale =>
                        sale.CancelledPaymentCount),

                ActiveRefundPaymentCount =
                    sales.Sum(sale =>
                        sale.ActiveRefundPaymentCount),

                CancelledRefundPaymentCount =
                    sales.Sum(sale =>
                        sale.CancelledRefundPaymentCount)
            };

            return Ok(new
            {
                Customer = customer,

                DateFilter = new
                {
                    StartDate = startDate,
                    EndDate = endDate
                },

                Summary = summary,
                Sales = sales
            });
        }
    }
}