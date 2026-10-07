using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/SaleReturns")]
    [ApiController]
    public class SaleReturnApprovalsController
        : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleReturnApprovalsController(
            AppDbContext context)
        {
            _context = context;
        }

        // Taslak satış iadesini onaylar ve ürünleri stoğa ekler
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> ApproveSaleReturn(int id)
        {
            var saleReturn = await _context.SaleReturns
                .Include(saleReturn => saleReturn.Sale)
                .Include(saleReturn =>
                    saleReturn.SaleReturnItems)
                    .ThenInclude(item => item.Product)
                .Include(saleReturn =>
                    saleReturn.SaleReturnItems)
                    .ThenInclude(item => item.SaleItem)
                .FirstOrDefaultAsync(saleReturn =>
                    saleReturn.Id == id);

            if (saleReturn is null)
            {
                return NotFound(new
                {
                    Message = "Satış iadesi bulunamadı."
                });
            }

            if (saleReturn.Status ==
                SaleReturnStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış iadesi zaten onaylanmış."
                });
            }

            if (saleReturn.Status ==
                SaleReturnStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satış iadesi onaylanamaz."
                });
            }

            if (saleReturn.Status !=
                SaleReturnStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca taslak satış iadeleri onaylanabilir."
                });
            }

            if (saleReturn.Sale.Status !=
                SaleStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca onaylanmış satışlara ait iadeler onaylanabilir."
                });
            }

            if (saleReturn.SaleReturnItems.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Ürün bulunmayan satış iadesi onaylanamaz."
                });
            }

            var saleItemIds = saleReturn
                .SaleReturnItems
                .Select(item => item.SaleItemId)
                .ToList();

            var approvedReturnQuantities =
                await _context.SaleReturnItems
                    .AsNoTracking()
                    .Where(item =>
                        saleItemIds.Contains(
                            item.SaleItemId) &&
                        item.SaleReturnId !=
                            saleReturn.Id &&
                        item.SaleReturn.Status ==
                            SaleReturnStatus.Approved)
                    .GroupBy(item => item.SaleItemId)
                    .Select(group => new
                    {
                        SaleItemId = group.Key,
                        ReturnedQuantity =
                            group.Sum(item =>
                                item.Quantity)
                    })
                    .ToDictionaryAsync(
                        item => item.SaleItemId,
                        item => item.ReturnedQuantity);

            foreach (var returnItem in
                     saleReturn.SaleReturnItems)
            {
                int previouslyApprovedQuantity =
                    approvedReturnQuantities
                        .GetValueOrDefault(
                            returnItem.SaleItemId,
                            0);

                int remainingReturnableQuantity =
                    returnItem.SaleItem.Quantity -
                    previouslyApprovedQuantity;

                if (returnItem.Quantity >
                    remainingReturnableQuantity)
                {
                    return BadRequest(new
                    {
                        Message =
                            $"'{returnItem.Product.Name}' ürünü için iade miktarı kalan iade edilebilir miktarı geçemez.",
                        returnItem.ProductId,
                        ProductName =
                            returnItem.Product.Name,
                        SoldQuantity =
                            returnItem.SaleItem.Quantity,
                        PreviouslyApprovedReturnQuantity =
                            previouslyApprovedQuantity,
                        RemainingReturnableQuantity =
                            remainingReturnableQuantity,
                        RequestedReturnQuantity =
                            returnItem.Quantity
                    });
                }
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            foreach (var returnItem in
                     saleReturn.SaleReturnItems)
            {
                int previousStock =
                    returnItem.Product.StockQuantity;

                returnItem.Product.StockQuantity +=
                    returnItem.Quantity;

                var stockMovement = new StockMovement
                {
                    ProductId = returnItem.ProductId,
                    MovementType = "İade Giriş",
                    Quantity = returnItem.Quantity,
                    Description =
                        $"Satış iadesi - İade No: {saleReturn.ReturnNumber} - Fatura: {saleReturn.Sale.InvoiceNumber}",
                    PreviousStock = previousStock,
                    CurrentStock =
                        returnItem.Product.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.StockMovements
                    .AddAsync(stockMovement);
            }

            saleReturn.Status =
                SaleReturnStatus.Approved;

            saleReturn.ApprovedAt =
                DateTime.UtcNow;

            saleReturn.Sale.ReturnedAmount +=
                saleReturn.TotalRefundAmount;

            saleReturn.Sale.ReturnedProfitAmount +=
                saleReturn.TotalProfitAdjustment;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                Message =
                    "Satış iadesi onaylandı ve ürünler stoğa geri eklendi.",

                SaleReturn = new
                {
                    saleReturn.Id,
                    saleReturn.SaleId,
                    saleReturn.ReturnNumber,
                    saleReturn.Status,
                    saleReturn.ApprovedAt,
                    saleReturn.TotalRefundAmount,
                    saleReturn.TotalCostAmount,
                    saleReturn.TotalProfitAdjustment
                },

                SaleSummary = new
                {
                    saleReturn.Sale.Id,
                    saleReturn.Sale.InvoiceNumber,
                    saleReturn.Sale.TotalAmount,
                    saleReturn.Sale.ReturnedAmount,
                    saleReturn.Sale.NetAmount,
                    saleReturn.Sale.PaidAmount,
                    saleReturn.Sale.RemainingAmount,
                    saleReturn.Sale.RefundDueAmount,
                    saleReturn.Sale.TotalProfit,
                    saleReturn.Sale.ReturnedProfitAmount,
                    saleReturn.Sale.NetProfit,
                    saleReturn.Sale.PaymentStatus
                },

                UpdatedProducts =
                    saleReturn.SaleReturnItems
                        .Select(item => new
                        {
                            item.ProductId,
                            ProductName =
                                item.Product.Name,
                            ReturnedQuantity =
                                item.Quantity,
                            PreviousStock =
                                item.Product.StockQuantity -
                                item.Quantity,
                            CurrentStock =
                                item.Product.StockQuantity
                        })
            });
        }
    }
}