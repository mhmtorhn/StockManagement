using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.DTOs;
using StockManagement.Entities;

namespace StockManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchasesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchasesController(AppDbContext context)
        {
            _context = context;
        }

        // Satın alma kayıtlarını listeler ve filtreler
        [HttpGet]
        public async Task<IActionResult> GetPurchases(
            int? supplierId,
            PurchaseStatus? status,
            PaymentMethod? paymentMethod,
            PaymentStatus? paymentStatus,
            bool? isOverdue,
            DateTime? startDate,
            DateTime? endDate)
        {
            IQueryable<Purchase> query = _context.Purchases
                .AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.Status == status.Value);
            }
            else
            {
                query = query.Where(purchase =>
                    purchase.Status != PurchaseStatus.Cancelled);
            }

            if (supplierId.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.SupplierId == supplierId.Value);
            }

            if (paymentMethod.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.PaymentMethod == paymentMethod.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.PaymentStatus == paymentStatus.Value);
            }

            if (isOverdue.HasValue)
            {
                DateTime now = DateTime.UtcNow;

                if (isOverdue.Value)
                {
                    query = query.Where(purchase =>
                        purchase.Status == PurchaseStatus.Approved &&
                        purchase.RemainingAmount > 0 &&
                        purchase.DueDate.HasValue &&
                        purchase.DueDate.Value < now);
                }
                else
                {
                    query = query.Where(purchase =>
                        purchase.Status != PurchaseStatus.Approved ||
                        purchase.RemainingAmount <= 0 ||
                        !purchase.DueDate.HasValue ||
                        purchase.DueDate.Value >= now);
                }
            }

            if (startDate.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.PurchaseDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(purchase =>
                    purchase.PurchaseDate <= endDate.Value);
            }

            var purchases = await query
                .OrderByDescending(purchase => purchase.Id)
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
                    purchase.Status,
                    purchase.Description,
                    purchase.CreatedAt,
                    purchase.ApprovedAt,
                    purchase.CancelledAt,
                    purchase.CancellationReason,
                    IsOverdue =
                        purchase.Status == PurchaseStatus.Approved &&
                        purchase.RemainingAmount > 0 &&
                        purchase.DueDate.HasValue &&
                        purchase.DueDate.Value < DateTime.UtcNow,
                    OverdueDays =
                        purchase.Status == PurchaseStatus.Approved &&
                        purchase.RemainingAmount > 0 &&
                        purchase.DueDate.HasValue &&
                        purchase.DueDate.Value < DateTime.UtcNow
                            ? (int?)(DateTime.UtcNow.Date -
                                purchase.DueDate.Value.Date).Days
                            : null,
                    ItemCount = purchase.PurchaseItems.Count,
                    PaymentCount = purchase.PurchasePayments
                        .Count(payment => !payment.IsCancelled)
                })
                .ToListAsync();

            return Ok(purchases);
        }

        // ID numarasına göre satın alma detayını getirir
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPurchaseById(int id)
        {
            var purchase = await _context.Purchases
                .AsNoTracking()
                .Where(purchase => purchase.Id == id)
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
                    purchase.Status,
                    purchase.Description,
                    purchase.CreatedAt,
                    purchase.ApprovedAt,
                    purchase.CancelledAt,
                    purchase.CancellationReason,
                    IsOverdue =
                        purchase.Status == PurchaseStatus.Approved &&
                        purchase.RemainingAmount > 0 &&
                        purchase.DueDate.HasValue &&
                        purchase.DueDate.Value < DateTime.UtcNow,
                    OverdueDays =
                        purchase.Status == PurchaseStatus.Approved &&
                        purchase.RemainingAmount > 0 &&
                        purchase.DueDate.HasValue &&
                        purchase.DueDate.Value < DateTime.UtcNow
                            ? (int?)(DateTime.UtcNow.Date -
                                purchase.DueDate.Value.Date).Days
                            : null,
                    Items = purchase.PurchaseItems
                        .OrderBy(item => item.Id)
                        .Select(item => new
                        {
                            item.Id,
                            item.ProductId,
                            ProductName = item.Product.Name,
                            ProductBarcode = item.Product.Barcode,
                            item.Quantity,
                            item.UnitPrice,
                            item.TotalPrice
                        })
                        .ToList(),
                    Payments = purchase.PurchasePayments
                        .OrderByDescending(payment =>
                            payment.PaymentDate)
                        .ThenByDescending(payment => payment.Id)
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
                .FirstOrDefaultAsync();

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            return Ok(purchase);
        }

        // Stokları değiştirmeden taslak satın alma oluşturur
        [HttpPost]
        public async Task<IActionResult> CreatePurchase(
            CreatePurchaseDto dto)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(supplier =>
                    supplier.Id == dto.SupplierId);

            if (supplier is null)
            {
                return BadRequest(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            if (!supplier.IsActive)
            {
                return BadRequest(new
                {
                    Message =
                        "Pasif bir tedarikçi için satın alma oluşturulamaz."
                });
            }

            string invoiceNumber = dto.InvoiceNumber.Trim();

            bool invoiceExists = await _context.Purchases
                .AnyAsync(purchase =>
                    purchase.SupplierId == dto.SupplierId &&
                    purchase.InvoiceNumber == invoiceNumber);

            if (invoiceExists)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu tedarikçiye ait aynı fatura numarası zaten bulunuyor."
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

            DateTime purchaseDate =
                dto.PurchaseDate ?? DateTime.UtcNow;

            if (dto.DueDate.HasValue &&
                dto.DueDate.Value < purchaseDate)
            {
                return BadRequest(new
                {
                    Message =
                        "Vade tarihi satın alma tarihinden önce olamaz."
                });
            }

            if (dto.Items.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Satın alma işleminde en az bir ürün olmalıdır."
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
                        "Aynı ürün satın alma listesine birden fazla kez eklenemez."
                });
            }

            var productIds = dto.Items
                .Select(item => item.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return BadRequest(new
                {
                    Message =
                        "Satın alma listesindeki ürünlerden biri bulunamadı."
                });
            }

            var passiveProduct = products
                .FirstOrDefault(product => !product.IsActive);

            if (passiveProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{passiveProduct.Name}' ürünü pasif durumdadır."
                });
            }

            var differentSupplierProduct = products
                .FirstOrDefault(product =>
                    product.SupplierId != dto.SupplierId);

            if (differentSupplierProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{differentSupplierProduct.Name}' ürünü seçilen tedarikçiye ait değildir."
                });
            }

            var purchase = new Purchase
            {
                SupplierId = dto.SupplierId,
                InvoiceNumber = invoiceNumber,
                PurchaseDate = purchaseDate,
                DueDate = dto.DueDate,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = PaymentStatus.Unpaid,
                PaidAmount = 0,
                Status = PurchaseStatus.Draft,
                Description = dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var itemDto in dto.Items)
            {
                decimal totalPrice =
                    itemDto.Quantity * itemDto.UnitPrice;

                var purchaseItem = new PurchaseItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = totalPrice
                };

                purchase.PurchaseItems.Add(purchaseItem);
            }

            purchase.TotalAmount = purchase.PurchaseItems
                .Sum(item => item.TotalPrice);

            purchase.RemainingAmount = purchase.TotalAmount;

            await _context.Purchases.AddAsync(purchase);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                Message =
                    "Taslak satın alma başarıyla oluşturuldu.",
                Purchase = new
                {
                    purchase.Id,
                    purchase.SupplierId,
                    SupplierName = supplier.Name,
                    purchase.InvoiceNumber,
                    purchase.PurchaseDate,
                    purchase.DueDate,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentMethod,
                    purchase.PaymentStatus,
                    purchase.Status,
                    purchase.Description,
                    purchase.CreatedAt,
                    Items = purchase.PurchaseItems.Select(item =>
                    {
                        var product = products.First(product =>
                            product.Id == item.ProductId);

                        return new
                        {
                            item.Id,
                            item.ProductId,
                            ProductName = product.Name,
                            item.Quantity,
                            item.UnitPrice,
                            item.TotalPrice
                        };
                    })
                }
            });
        }

        // Taslak satın alma kaydını stokları etkilemeden günceller
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePurchase(
            int id,
            UpdatePurchaseDto dto)
        {
            var purchase = await _context.Purchases
                .Include(purchase => purchase.PurchaseItems)
                .FirstOrDefaultAsync(purchase =>
                    purchase.Id == id);

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            if (purchase.Status == PurchaseStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Onaylanmış satın alma kaydı güncellenemez."
                });
            }

            if (purchase.Status == PurchaseStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satın alma kaydı güncellenemez."
                });
            }

            if (purchase.Status != PurchaseStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca taslak satın alma kayıtları güncellenebilir."
                });
            }

            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(supplier =>
                    supplier.Id == dto.SupplierId);

            if (supplier is null)
            {
                return BadRequest(new
                {
                    Message = "Tedarikçi bulunamadı."
                });
            }

            if (!supplier.IsActive)
            {
                return BadRequest(new
                {
                    Message =
                        "Pasif bir tedarikçi için satın alma güncellenemez."
                });
            }

            string invoiceNumber = dto.InvoiceNumber.Trim();

            bool invoiceExists = await _context.Purchases
                .AnyAsync(otherPurchase =>
                    otherPurchase.Id != id &&
                    otherPurchase.SupplierId == dto.SupplierId &&
                    otherPurchase.InvoiceNumber == invoiceNumber);

            if (invoiceExists)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu tedarikçiye ait aynı fatura numarası zaten bulunuyor."
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

            DateTime updatedPurchaseDate =
                dto.PurchaseDate ?? purchase.PurchaseDate;

            if (dto.DueDate.HasValue &&
                dto.DueDate.Value < updatedPurchaseDate)
            {
                return BadRequest(new
                {
                    Message =
                        "Vade tarihi satın alma tarihinden önce olamaz."
                });
            }

            if (dto.Items.Count == 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Satın alma işleminde en az bir ürün olmalıdır."
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
                        "Aynı ürün satın alma listesine birden fazla kez eklenemez."
                });
            }

            var productIds = dto.Items
                .Select(item => item.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return BadRequest(new
                {
                    Message =
                        "Satın alma listesindeki ürünlerden biri bulunamadı."
                });
            }

            var passiveProduct = products
                .FirstOrDefault(product => !product.IsActive);

            if (passiveProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{passiveProduct.Name}' ürünü pasif durumdadır."
                });
            }

            var differentSupplierProduct = products
                .FirstOrDefault(product =>
                    product.SupplierId != dto.SupplierId);

            if (differentSupplierProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{differentSupplierProduct.Name}' ürünü seçilen tedarikçiye ait değildir."
                });
            }

            purchase.SupplierId = dto.SupplierId;
            purchase.InvoiceNumber = invoiceNumber;
            purchase.PurchaseDate = updatedPurchaseDate;
            purchase.DueDate = dto.DueDate;
            purchase.PaymentMethod = dto.PaymentMethod;
            purchase.Description = dto.Description.Trim();

            var existingItems = purchase.PurchaseItems.ToList();

            _context.RemoveRange(existingItems);
            purchase.PurchaseItems.Clear();

            foreach (var itemDto in dto.Items)
            {
                decimal totalPrice =
                    itemDto.Quantity * itemDto.UnitPrice;

                var purchaseItem = new PurchaseItem
                {
                    PurchaseId = purchase.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = totalPrice
                };

                purchase.PurchaseItems.Add(purchaseItem);
            }

            purchase.TotalAmount = purchase.PurchaseItems
                .Sum(item => item.TotalPrice);

            purchase.PaidAmount = 0;
            purchase.RemainingAmount = purchase.TotalAmount;
            purchase.PaymentStatus = PaymentStatus.Unpaid;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Taslak satın alma başarıyla güncellendi.",
                Purchase = new
                {
                    purchase.Id,
                    purchase.SupplierId,
                    SupplierName = supplier.Name,
                    purchase.InvoiceNumber,
                    purchase.PurchaseDate,
                    purchase.DueDate,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentMethod,
                    purchase.PaymentStatus,
                    purchase.Status,
                    purchase.Description,
                    purchase.CreatedAt,
                    Items = purchase.PurchaseItems.Select(item =>
                    {
                        var product = products.First(product =>
                            product.Id == item.ProductId);

                        return new
                        {
                            item.Id,
                            item.ProductId,
                            ProductName = product.Name,
                            item.Quantity,
                            item.UnitPrice,
                            item.TotalPrice
                        };
                    })
                }
            });
        }

        // Taslak satın almayı onaylar ve stoklara işler
        [HttpPut("{id:int}/approve")]
        public async Task<IActionResult> ApprovePurchase(int id)
        {
            var purchase = await _context.Purchases
                .Include(purchase => purchase.Supplier)
                .Include(purchase => purchase.PurchaseItems)
                    .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(purchase =>
                    purchase.Id == id);

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            if (purchase.Status == PurchaseStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Satın alma kaydı zaten onaylanmış."
                });
            }

            if (purchase.Status == PurchaseStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satın alma onaylanamaz."
                });
            }

            if (!purchase.Supplier.IsActive)
            {
                return BadRequest(new
                {
                    Message =
                        "Pasif tedarikçiye ait satın alma onaylanamaz."
                });
            }

            var passiveProduct = purchase.PurchaseItems
                .Select(item => item.Product)
                .FirstOrDefault(product => !product.IsActive);

            if (passiveProduct is not null)
            {
                return BadRequest(new
                {
                    Message =
                        $"'{passiveProduct.Name}' ürünü pasif olduğu için satın alma onaylanamaz."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            foreach (var item in purchase.PurchaseItems)
            {
                int previousStock = item.Product.StockQuantity;

                item.Product.StockQuantity += item.Quantity;
                item.Product.PurchasePrice = item.UnitPrice;

                var stockMovement = new StockMovement
                {
                    ProductId = item.ProductId,
                    MovementType = "Giriş",
                    Quantity = item.Quantity,
                    Description =
                        $"Satın alma onayı - Fatura: {purchase.InvoiceNumber}",
                    PreviousStock = previousStock,
                    CurrentStock = item.Product.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.StockMovements
                    .AddAsync(stockMovement);
            }

            purchase.Status = PurchaseStatus.Approved;
            purchase.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                Message =
                    "Satın alma onaylandı ve stoklara işlendi.",
                purchase.Id,
                purchase.InvoiceNumber,
                purchase.Status,
                purchase.ApprovedAt,
                purchase.DueDate,
                purchase.TotalAmount,
                purchase.PaidAmount,
                purchase.RemainingAmount,
                purchase.PaymentStatus,
                UpdatedProducts = purchase.PurchaseItems.Select(item =>
                    new
                    {
                        item.ProductId,
                        ProductName = item.Product.Name,
                        AddedQuantity = item.Quantity,
                        CurrentStock = item.Product.StockQuantity,
                        PurchasePrice = item.Product.PurchasePrice
                    })
            });
        }

        // Taslak satın alma kaydını stokları etkilemeden iptal eder
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> CancelPurchase(
            int id,
            CancelPurchaseDto dto)
        {
            var purchase = await _context.Purchases
                .FirstOrDefaultAsync(purchase =>
                    purchase.Id == id);

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            if (purchase.Status == PurchaseStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Onaylanmış satın alma kaydı iptal edilemez."
                });
            }

            if (purchase.Status == PurchaseStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "Satın alma kaydı zaten iptal edilmiş."
                });
            }

            if (purchase.Status != PurchaseStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca taslak satın alma kayıtları iptal edilebilir."
                });
            }

            string cancellationReason =
                dto.CancellationReason.Trim();

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                return BadRequest(new
                {
                    Message = "İptal nedeni zorunludur."
                });
            }

            purchase.Status = PurchaseStatus.Cancelled;
            purchase.CancelledAt = DateTime.UtcNow;
            purchase.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Taslak satın alma başarıyla iptal edildi.",
                purchase.Id,
                purchase.InvoiceNumber,
                purchase.Status,
                purchase.CancelledAt,
                purchase.CancellationReason
            });
        }

        // Onaylanmış satın alma kaydına ödeme ekler
        [HttpPost("{id:int}/payments")]
        public async Task<IActionResult> AddPurchasePayment(
            int id,
            CreatePurchasePaymentDto dto)
        {
            var purchase = await _context.Purchases
                .FirstOrDefaultAsync(purchase =>
                    purchase.Id == id);

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            if (purchase.Status == PurchaseStatus.Draft)
            {
                return BadRequest(new
                {
                    Message =
                        "Taslak satın alma kaydına ödeme eklenemez. Önce satın alma onaylanmalıdır."
                });
            }

            if (purchase.Status == PurchaseStatus.Cancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "İptal edilmiş satın alma kaydına ödeme eklenemez."
                });
            }

            if (purchase.Status != PurchaseStatus.Approved)
            {
                return BadRequest(new
                {
                    Message =
                        "Yalnızca onaylanmış satın alma kayıtlarına ödeme eklenebilir."
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
                        "Ödeme tutarı 0'dan büyük olmalıdır."
                });
            }

            if (purchase.PaymentStatus == PaymentStatus.Paid ||
                purchase.RemainingAmount <= 0)
            {
                return BadRequest(new
                {
                    Message =
                        "Bu satın alma kaydının borcu tamamen ödenmiş."
                });
            }

            if (dto.Amount > purchase.RemainingAmount)
            {
                return BadRequest(new
                {
                    Message =
                        $"Ödeme tutarı kalan borçtan fazla olamaz. Kalan borç: {purchase.RemainingAmount}"
                });
            }

            var payment = new PurchasePayment
            {
                PurchaseId = purchase.Id,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                PaymentDate =
                    dto.PaymentDate ?? DateTime.UtcNow,
                ReferenceNumber =
                    dto.ReferenceNumber.Trim(),
                Description = dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            purchase.PaidAmount += payment.Amount;
            purchase.RemainingAmount =
                purchase.TotalAmount - purchase.PaidAmount;

            if (purchase.RemainingAmount == 0)
            {
                purchase.PaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                purchase.PaymentStatus =
                    PaymentStatus.PartiallyPaid;
            }

            await _context.PurchasePayments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                Message = purchase.PaymentStatus ==
                          PaymentStatus.Paid
                    ? "Ödeme kaydedildi. Satın alma borcu tamamen ödendi."
                    : "Kısmi ödeme başarıyla kaydedildi.",
                Payment = new
                {
                    payment.Id,
                    payment.PurchaseId,
                    payment.Amount,
                    payment.PaymentMethod,
                    payment.PaymentDate,
                    payment.ReferenceNumber,
                    payment.Description,
                    payment.CreatedAt
                },
                PurchaseSummary = new
                {
                    purchase.Id,
                    purchase.InvoiceNumber,
                    purchase.DueDate,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentStatus
                }
            });
        }

        // Satın alma kaydının ödeme geçmişini listeler
        [HttpGet("{id:int}/payments")]
        public async Task<IActionResult> GetPurchasePayments(int id)
        {
            var purchase = await _context.Purchases
                .AsNoTracking()
                .Where(purchase => purchase.Id == id)
                .Select(purchase => new
                {
                    purchase.Id,
                    purchase.InvoiceNumber,
                    purchase.SupplierId,
                    SupplierName = purchase.Supplier.Name,
                    purchase.DueDate,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentStatus,
                    Payments = purchase.PurchasePayments
                        .OrderByDescending(payment =>
                            payment.PaymentDate)
                        .ThenByDescending(payment => payment.Id)
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
                .FirstOrDefaultAsync();

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            return Ok(purchase);
        }

        // Ödeme kaydını silmeden iptal eder ve borcu yeniden hesaplar
        [HttpPut("{id:int}/payments/{paymentId:int}/cancel")]
        public async Task<IActionResult> CancelPurchasePayment(
            int id,
            int paymentId,
            CancelPurchasePaymentDto dto)
        {
            var purchase = await _context.Purchases
                .Include(purchase => purchase.PurchasePayments)
                .FirstOrDefaultAsync(purchase =>
                    purchase.Id == id);

            if (purchase is null)
            {
                return NotFound(new
                {
                    Message = "Satın alma kaydı bulunamadı."
                });
            }

            var payment = purchase.PurchasePayments
                .FirstOrDefault(payment =>
                    payment.Id == paymentId);

            if (payment is null)
            {
                return NotFound(new
                {
                    Message =
                        "Bu satın alma kaydına ait ödeme bulunamadı."
                });
            }

            if (payment.IsCancelled)
            {
                return BadRequest(new
                {
                    Message =
                        "Ödeme kaydı zaten iptal edilmiş."
                });
            }

            string cancellationReason =
                dto.CancellationReason.Trim();

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                return BadRequest(new
                {
                    Message =
                        "Ödeme iptal nedeni zorunludur."
                });
            }

            payment.IsCancelled = true;
            payment.CancelledAt = DateTime.UtcNow;
            payment.CancellationReason = cancellationReason;

            purchase.PaidAmount = purchase.PurchasePayments
                .Where(existingPayment =>
                    !existingPayment.IsCancelled)
                .Sum(existingPayment =>
                    existingPayment.Amount);

            purchase.RemainingAmount =
                purchase.TotalAmount - purchase.PaidAmount;

            if (purchase.PaidAmount == 0)
            {
                purchase.PaymentStatus = PaymentStatus.Unpaid;
            }
            else if (purchase.RemainingAmount == 0)
            {
                purchase.PaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                purchase.PaymentStatus =
                    PaymentStatus.PartiallyPaid;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message =
                    "Ödeme kaydı başarıyla iptal edildi.",
                Payment = new
                {
                    payment.Id,
                    payment.PurchaseId,
                    payment.Amount,
                    payment.PaymentMethod,
                    payment.IsCancelled,
                    payment.CancelledAt,
                    payment.CancellationReason
                },
                PurchaseSummary = new
                {
                    purchase.Id,
                    purchase.InvoiceNumber,
                    purchase.TotalAmount,
                    purchase.PaidAmount,
                    purchase.RemainingAmount,
                    purchase.PaymentStatus
                }
            });
        }
    }
}