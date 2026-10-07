using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleReturnsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SaleReturnsController(AppDbContext context)
        {
            _context = context;
        }

        // Satış iadelerini listeler ve filtreler
        [HttpGet]
        public async Task<IActionResult> GetSaleReturns(
            int? saleId,
            int? customerId,
            SaleReturnStatus? status,
            DateTime? startDate,
            DateTime? endDate)
        {
            IQueryable<SaleReturn> query =
                _context.SaleReturns.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(saleReturn =>
                    saleReturn.Status == status.Value);
            }
            else
            {
                query = query.Where(saleReturn =>
                    saleReturn.Status !=
                        SaleReturnStatus.Cancelled);
            }

            if (saleId.HasValue)
            {
                query = query.Where(saleReturn =>
                    saleReturn.SaleId == saleId.Value);
            }

            if (customerId.HasValue)
            {
                query = query.Where(saleReturn =>
                    saleReturn.Sale.CustomerId ==
                        customerId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(saleReturn =>
                    saleReturn.ReturnDate >=
                        startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(saleReturn =>
                    saleReturn.ReturnDate <=
                        endDate.Value);
            }

            var saleReturns = await query
                .OrderByDescending(saleReturn =>
                    saleReturn.Id)
                .Select(saleReturn => new
                {
                    saleReturn.Id,
                    saleReturn.SaleId,
                    saleReturn.Sale.InvoiceNumber,
                    saleReturn.Sale.CustomerId,
                    CustomerName =
                        saleReturn.Sale.Customer.Name,
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
                    ItemCount =
                        saleReturn.SaleReturnItems.Count
                })
                .ToListAsync();

            return Ok(saleReturns);
        }

        // ID numarasına göre satış iadesi detayını getirir
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSaleReturnById(int id)
        {
            var saleReturn = await _context.SaleReturns
                .AsNoTracking()
                .Where(saleReturn =>
                    saleReturn.Id == id)
                .Select(saleReturn => new
                {
                    saleReturn.Id,
                    saleReturn.SaleId,
                    saleReturn.Sale.InvoiceNumber,
                    saleReturn.Sale.CustomerId,
                    CustomerName =
                        saleReturn.Sale.Customer.Name,
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

                    Items = saleReturn.SaleReturnItems
                        .OrderBy(item => item.Id)
                        .Select(item => new
                        {
                            item.Id,
                            item.SaleItemId,
                            item.ProductId,
                            ProductName =
                                item.Product.Name,
                            ProductBarcode =
                                item.Product.Barcode,
                            item.Quantity,
                            item.UnitPrice,
                            item.CostPrice,
                            item.RefundAmount,
                            item.ProfitAdjustment
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (saleReturn is null)
            {
                return NotFound(new
                {
                    Message =
                        "Satış iadesi bulunamadı."
                });
            }

            return Ok(saleReturn);
        }

        // Stokları değiştirmeden taslak satış iadesi oluşturur
        [HttpPost]
        public async Task<IActionResult> CreateSaleReturn(
            CreateSaleReturnDto dto)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Include(sale => sale.Customer)
                .Include(sale => sale.SaleItems)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(sale =>
                    sale.Id == dto.SaleId);

            if (sale is null)
            {
                return BadRequest(new
                {
                    Message = "Satış kaydı bulunamadı."
                });
            }

            if (sale.Status != SaleStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca onaylanmış satışlar için iade oluşturulabilir."
                });
            }

            string returnNumber =
                dto.ReturnNumber.Trim();

            if (string.IsNullOrWhiteSpace(returnNumber))
            {
                return BadRequest(new
                {
                    Message = "İade numarası zorunludur."
                });
            }

            bool returnNumberExists =
                await _context.SaleReturns
                    .AnyAsync(saleReturn =>
                        saleReturn.ReturnNumber ==
                            returnNumber);

            if (returnNumberExists)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu iade numarasına ait kayıt zaten bulunuyor."
                });
            }

            string reason = dto.Reason.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new
                {
                    Message = "İade nedeni zorunludur."
                });
            }

            DateTime returnDate =
                dto.ReturnDate ?? DateTime.UtcNow;

            if (returnDate < sale.SaleDate)
            {
                return BadRequest(new
                {
                    Message =
                        "İade tarihi satış tarihinden önce olamaz."
                });
            }

            if (dto.Items.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "İade işleminde en az bir ürün olmalıdır."
                });
            }

            bool hasDuplicateSaleItem = dto.Items
                .GroupBy(item => item.SaleItemId)
                .Any(group => group.Count() > 1);

            if (hasDuplicateSaleItem)
            {
                return BadRequest(new
                {
                    Message =
                        "Aynı satış kalemi iade listesine birden fazla kez eklenemez."
                });
            }

            var requestedSaleItemIds = dto.Items
                .Select(item => item.SaleItemId)
                .ToList();

            var saleItems = sale.SaleItems
                .Where(item =>
                    requestedSaleItemIds.Contains(item.Id))
                .ToList();

            if (saleItems.Count !=
                requestedSaleItemIds.Count)
            {
                return BadRequest(new
                {
                    Message =
                        "İade listesindeki satış kalemlerinden biri bu satışa ait değil."
                });
            }

            var previouslyReturnedQuantities =
                await _context.SaleReturnItems
                    .AsNoTracking()
                    .Where(returnItem =>
                        requestedSaleItemIds.Contains(
                            returnItem.SaleItemId) &&
                        returnItem.SaleReturn.Status !=
                            SaleReturnStatus.Cancelled)
                    .GroupBy(returnItem =>
                        returnItem.SaleItemId)
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

            foreach (var itemDto in dto.Items)
            {
                var saleItem = saleItems.First(item =>
                    item.Id == itemDto.SaleItemId);

                int previouslyReturned =
                    previouslyReturnedQuantities
                        .GetValueOrDefault(
                            itemDto.SaleItemId,
                            0);

                int remainingReturnableQuantity =
                    saleItem.Quantity -
                    previouslyReturned;

                if (itemDto.Quantity >
                    remainingReturnableQuantity)
                {
                    return BadRequest(new
                    {
                        Message =
                            $"'{saleItem.Product.Name}' ürünü için iade miktarı kalan iade edilebilir miktarı geçemez.",
                        saleItem.ProductId,
                        ProductName =
                            saleItem.Product.Name,
                        SoldQuantity =
                            saleItem.Quantity,
                        PreviouslyReturnedQuantity =
                            previouslyReturned,
                        RemainingReturnableQuantity =
                            remainingReturnableQuantity,
                        RequestedReturnQuantity =
                            itemDto.Quantity
                    });
                }
            }

            var saleReturn = new SaleReturn
            {
                SaleId = sale.Id,
                ReturnNumber = returnNumber,
                ReturnDate = returnDate,
                Reason = reason,
                Status = SaleReturnStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var itemDto in dto.Items)
            {
                var saleItem = saleItems.First(item =>
                    item.Id == itemDto.SaleItemId);

                decimal refundAmount =
                    itemDto.Quantity *
                    saleItem.UnitPrice;

                decimal costAmount =
                    itemDto.Quantity *
                    saleItem.CostPrice;

                decimal profitAdjustment =
                    refundAmount - costAmount;

                var returnItem = new SaleReturnItem
                {
                    SaleItemId = saleItem.Id,
                    ProductId = saleItem.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = saleItem.UnitPrice,
                    CostPrice = saleItem.CostPrice,
                    RefundAmount = refundAmount,
                    ProfitAdjustment =
                        profitAdjustment
                };

                saleReturn.SaleReturnItems.Add(
                    returnItem);
            }

            saleReturn.TotalRefundAmount =
                saleReturn.SaleReturnItems
                    .Sum(item =>
                        item.RefundAmount);

            saleReturn.TotalCostAmount =
                saleReturn.SaleReturnItems
                    .Sum(item =>
                        item.CostPrice *
                        item.Quantity);

            saleReturn.TotalProfitAdjustment =
                saleReturn.SaleReturnItems
                    .Sum(item =>
                        item.ProfitAdjustment);

            await _context.SaleReturns.AddAsync(
                saleReturn);

            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    Message =
                        "Taslak satış iadesi başarıyla oluşturuldu.",

                    SaleReturn = new
                    {
                        saleReturn.Id,
                        saleReturn.SaleId,
                        sale.InvoiceNumber,
                        sale.CustomerId,
                        CustomerName =
                            sale.Customer.Name,
                        saleReturn.ReturnNumber,
                        saleReturn.ReturnDate,
                        saleReturn.TotalRefundAmount,
                        saleReturn.TotalCostAmount,
                        saleReturn.TotalProfitAdjustment,
                        saleReturn.Reason,
                        saleReturn.Status,
                        saleReturn.CreatedAt,

                        Items =
                            saleReturn.SaleReturnItems
                                .Select(item =>
                                {
                                    var product =
                                        saleItems.First(
                                            saleItem =>
                                                saleItem.Id ==
                                                item.SaleItemId)
                                            .Product;

                                    return new
                                    {
                                        item.Id,
                                        item.SaleItemId,
                                        item.ProductId,
                                        ProductName =
                                            product.Name,
                                        item.Quantity,
                                        item.UnitPrice,
                                        item.CostPrice,
                                        item.RefundAmount,
                                        item.ProfitAdjustment
                                    };
                                })
                    }
                });
        }
    }
}