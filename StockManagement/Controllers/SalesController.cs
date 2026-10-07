using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

        // Satış kayıtlarını listeler ve filtreler
        [HttpGet]
        public async Task<IActionResult> GetSales(
            int? customerId,
            SaleStatus? status,
            PaymentMethod? paymentMethod,
            PaymentStatus? paymentStatus,
            DateTime? startDate,
            DateTime? endDate)
        {
            IQueryable<Sale> query = _context.Sales
                .AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(sale =>
                    sale.Status == status.Value);
            }
            else
            {
                query = query.Where(sale =>
                    sale.Status != SaleStatus.Cancelled);
            }

            if (customerId.HasValue)
            {
                query = query.Where(sale =>
                    sale.CustomerId == customerId.Value);
            }

            if (paymentMethod.HasValue)
            {
                query = query.Where(sale =>
                    sale.PaymentMethod == paymentMethod.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(sale =>
                    sale.PaymentStatus == paymentStatus.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(sale =>
                    sale.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(sale =>
                    sale.SaleDate <= endDate.Value);
            }

            var sales = await query
                .OrderByDescending(sale => sale.Id)
                .Select(sale => new
                {
                    sale.Id,
                    sale.CustomerId,
                    CustomerName = sale.Customer.Name,
                    sale.InvoiceNumber,
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
                    sale.Status,
                    sale.Description,
                    sale.CreatedAt,
                    sale.ApprovedAt,
                    sale.CancelledAt,
                    sale.CancellationReason,

                    ItemCount = sale.SaleItems.Count,

                    ActivePaymentCount =
                        sale.SalePayments.Count(payment =>
                            !payment.IsCancelled),

                    CancelledPaymentCount =
                        sale.SalePayments.Count(payment =>
                            payment.IsCancelled),

                    DraftReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                            SaleReturnStatus.Draft),

                    ApprovedReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                            SaleReturnStatus.Approved),

                    CancelledReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                            SaleReturnStatus.Cancelled),

                    ActiveRefundPaymentCount =
                        sale.SaleRefundPayments.Count(
                            refundPayment =>
                                !refundPayment.IsCancelled),

                    CancelledRefundPaymentCount =
                        sale.SaleRefundPayments.Count(
                            refundPayment =>
                                refundPayment.IsCancelled)
                })
                .ToListAsync();

            return Ok(sales);
        }

        // ID numarasına göre satış detayını getirir
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSaleById(int id)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Where(sale => sale.Id == id)
                .Select(sale => new
                {
                    sale.Id,
                    sale.CustomerId,
                    CustomerName = sale.Customer.Name,
                    CustomerType =
                        sale.Customer.CustomerType,
                    CustomerPhoneNumber =
                        sale.Customer.PhoneNumber,
                    CustomerEmail =
                        sale.Customer.Email,
                    CustomerAddress =
                        sale.Customer.Address,
                    CustomerTaxNumber =
                        sale.Customer.TaxNumber,

                    sale.InvoiceNumber,
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
                    sale.Status,
                    sale.Description,
                    sale.CreatedAt,
                    sale.ApprovedAt,
                    sale.CancelledAt,
                    sale.CancellationReason,

                    ActivePaymentCount =
                        sale.SalePayments.Count(payment =>
                            !payment.IsCancelled),

                    CancelledPaymentCount =
                        sale.SalePayments.Count(payment =>
                            payment.IsCancelled),

                    DraftReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                            SaleReturnStatus.Draft),

                    ApprovedReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                            SaleReturnStatus.Approved),

                    CancelledReturnCount =
                        sale.SaleReturns.Count(saleReturn =>
                            saleReturn.Status ==
                            SaleReturnStatus.Cancelled),

                    ActiveRefundPaymentCount =
                        sale.SaleRefundPayments.Count(
                            refundPayment =>
                                !refundPayment.IsCancelled),

                    CancelledRefundPaymentCount =
                        sale.SaleRefundPayments.Count(
                            refundPayment =>
                                refundPayment.IsCancelled),

                    Items = sale.SaleItems
                        .OrderBy(item => item.Id)
                        .Select(item => new
                        {
                            item.Id,
                            item.ProductId,
                            ProductName =
                                item.Product.Name,
                            ProductBarcode =
                                item.Product.Barcode,
                            item.Quantity,
                            item.UnitPrice,
                            item.CostPrice,
                            item.TotalPrice,
                            item.ProfitAmount
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

        // Stokları değiştirmeden taslak satış oluşturur
        [HttpPost]
        public async Task<IActionResult> CreateSale(
            CreateSaleDto dto)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(customer =>
                    customer.Id == dto.CustomerId);

            if (customer is null)
            {
                return BadRequest(new
                {
                    Message = "Müşteri bulunamadı."
                });
            }

            if (!customer.IsActive)
            {
                return BadRequest(new
                {
                    Message =
                        "Pasif müşteri için satış oluşturulamaz."
                });
            }

            string invoiceNumber =
                dto.InvoiceNumber.Trim();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                return BadRequest(new
                {
                    Message =
                        "Fatura numarası zorunludur."
                });
            }

            bool invoiceExists = await _context.Sales
                .AnyAsync(sale =>
                    sale.InvoiceNumber == invoiceNumber);

            if (invoiceExists)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu fatura numarasına ait satış zaten bulunuyor."
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

            DateTime saleDate =
                dto.SaleDate ?? DateTime.UtcNow;

            if (dto.DueDate.HasValue &&
                dto.DueDate.Value < saleDate)
            {
                return BadRequest(new
                {
                    Message =
                        "Vade tarihi satış tarihinden önce olamaz."
                });
            }

            if (dto.Items.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış işleminde en az bir ürün olmalıdır."
                });
            }

            bool hasDuplicateProduct = dto.Items
                .GroupBy(item => item.ProductId)
                .Any(group => group.Count() > 1);

            if (hasDuplicateProduct)
            {
                return BadRequest(new
                {
                    Message =
                        "Aynı ürün satış listesine birden fazla kez eklenemez."
                });
            }

            var productIds = dto.Items
                .Select(item => item.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(product =>
                    productIds.Contains(product.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış listesindeki ürünlerden biri bulunamadı."
                });
            }

            var passiveProduct = products
                .FirstOrDefault(product =>
                    !product.IsActive);

            if (passiveProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{passiveProduct.Name}' ürünü pasif durumdadır."
                });
            }

            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                InvoiceNumber = invoiceNumber,
                SaleDate = saleDate,
                DueDate = dto.DueDate,
                PaidAmount = 0,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = PaymentStatus.Unpaid,
                Status = SaleStatus.Draft,
                Description = dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var itemDto in dto.Items)
            {
                var product = products.First(product =>
                    product.Id == itemDto.ProductId);

                decimal totalPrice =
                    itemDto.Quantity * itemDto.UnitPrice;

                decimal profitAmount =
                    (itemDto.UnitPrice -
                        product.PurchasePrice) *
                    itemDto.Quantity;

                var saleItem = new SaleItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    CostPrice = product.PurchasePrice,
                    TotalPrice = totalPrice,
                    ProfitAmount = profitAmount
                };

                sale.SaleItems.Add(saleItem);
            }

            sale.TotalAmount = sale.SaleItems
                .Sum(item => item.TotalPrice);

            sale.RemainingAmount = sale.TotalAmount;

            sale.TotalProfit = sale.SaleItems
                .Sum(item => item.ProfitAmount);

            await _context.Sales.AddAsync(sale);
            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    Message =
                        "Taslak satış başarıyla oluşturuldu.",

                    Sale = new
                    {
                        sale.Id,
                        sale.CustomerId,
                        CustomerName = customer.Name,
                        sale.InvoiceNumber,
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
                        sale.Status,
                        sale.Description,
                        sale.CreatedAt,

                        Items = sale.SaleItems.Select(item =>
                        {
                            var product = products.First(
                                product =>
                                    product.Id ==
                                    item.ProductId);

                            return new
                            {
                                item.Id,
                                item.ProductId,
                                ProductName =
                                    product.Name,
                                item.Quantity,
                                item.UnitPrice,
                                item.CostPrice,
                                item.TotalPrice,
                                item.ProfitAmount
                            };
                        })
                    }
                });
        }

        // Taslak satışı onaylar ve ürün stoklarını düşürür
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> ApproveSale(int id)
        {
            var sale = await _context.Sales
                .Include(sale => sale.Customer)
                .Include(sale => sale.SaleItems)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(sale =>
                    sale.Id == id);

            if (sale is null)
            {
                return NotFound(new
                {
                    Message = "Satış kaydı bulunamadı."
                });
            }

            if (sale.Status == SaleStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Satış kaydı zaten onaylanmış."
                });
            }

            if (sale.Status == SaleStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satış onaylanamaz."
                });
            }

            if (sale.Status != SaleStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca taslak satışlar onaylanabilir."
                });
            }

            if (!sale.Customer.IsActive)
            {
                return BadRequest(new
                {
                    Message =
                        "Pasif müşteriye ait satış onaylanamaz."
                });
            }

            if (sale.SaleItems.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Ürün bulunmayan satış onaylanamaz."
                });
            }

            var passiveProduct = sale.SaleItems
                .Select(item => item.Product)
                .FirstOrDefault(product =>
                    !product.IsActive);

            if (passiveProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{passiveProduct.Name}' ürünü pasif olduğu için satış onaylanamaz."
                });
            }

            var insufficientStockItem =
                sale.SaleItems.FirstOrDefault(item =>
                    item.Product.StockQuantity <
                    item.Quantity);

            if (insufficientStockItem is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{insufficientStockItem.Product.Name}' ürünü için yeterli stok bulunmuyor.",

                    insufficientStockItem.ProductId,

                    ProductName =
                        insufficientStockItem.Product.Name,

                    RequestedQuantity =
                        insufficientStockItem.Quantity,

                    CurrentStock =
                        insufficientStockItem.Product
                            .StockQuantity
                });
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            foreach (var item in sale.SaleItems)
            {
                int previousStock =
                    item.Product.StockQuantity;

                item.CostPrice =
                    item.Product.PurchasePrice;

                item.TotalPrice =
                    item.Quantity * item.UnitPrice;

                item.ProfitAmount =
                    (item.UnitPrice - item.CostPrice) *
                    item.Quantity;

                item.Product.StockQuantity -=
                    item.Quantity;

                var stockMovement = new StockMovement
                {
                    ProductId = item.ProductId,
                    MovementType = "Çıkış",
                    Quantity = item.Quantity,
                    Description =
                        $"Satış onayı - Fatura: {sale.InvoiceNumber}",
                    PreviousStock = previousStock,
                    CurrentStock =
                        item.Product.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.StockMovements
                    .AddAsync(stockMovement);
            }

            sale.TotalAmount = sale.SaleItems
                .Sum(item => item.TotalPrice);

            sale.RemainingAmount =
                sale.TotalAmount - sale.PaidAmount;

            sale.TotalProfit = sale.SaleItems
                .Sum(item => item.ProfitAmount);

            sale.Status = SaleStatus.Approved;
            sale.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                Message =
                    "Satış onaylandı ve ürün stokları düşürüldü.",

                sale.Id,
                sale.InvoiceNumber,
                sale.Status,
                sale.ApprovedAt,

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

                UpdatedProducts =
                    sale.SaleItems.Select(item =>
                        new
                        {
                            item.ProductId,
                            ProductName =
                                item.Product.Name,
                            SoldQuantity =
                                item.Quantity,
                            PreviousStock =
                                item.Product
                                    .StockQuantity +
                                item.Quantity,
                            CurrentStock =
                                item.Product
                                    .StockQuantity,
                            item.UnitPrice,
                            item.CostPrice,
                            item.TotalPrice,
                            item.ProfitAmount
                        })
            });
        }
    }
}