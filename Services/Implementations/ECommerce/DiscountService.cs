using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using start.Data;
using start.Models;
using start.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace start.Services.Implementations.ECommerce
{
    public class DiscountService : IDiscountService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DiscountService> _logger;

        public DiscountService(ApplicationDbContext db, ILogger<DiscountService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> ApplyDiscountAsync(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("UserId và Code không được để trống.");
            }

            // Check if there's already a transaction (e.g., from OrderService)
            var existingTransaction = _db.Database.CurrentTransaction;
            bool ownsTransaction = existingTransaction == null;
            
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (ownsTransaction)
            {
                transaction = await _db.Database.BeginTransactionAsync();
                _logger.LogInformation($"Created new transaction for ApplyDiscountAsync");
            }
            else
            {
                _logger.LogInformation($"Using existing transaction for ApplyDiscountAsync");
            }
            
            try
            {
                // Simple query first to avoid SQL issues
                var discount = await _db.Discounts
                    .FirstOrDefaultAsync(d => d.Code == code && d.IsActive);

                if (discount == null)
                {
                    throw new Exception("Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa.");
                }

                // Check if discount is within valid date range
                var now = DateTime.Now;
                if (discount.StartAt.HasValue && discount.StartAt > now)
                {
                    throw new Exception("Mã giảm giá chưa đến thời gian sử dụng.");
                }

                if (discount.EndAt.HasValue && discount.EndAt < now)
                {
                    throw new Exception("Mã giảm giá đã hết hạn.");
                }

                // Check usage limit
                if (discount.UsageLimit.HasValue && discount.UsageLimit <= 0)
                {
                    throw new Exception("Mã giảm giá này đã được sử dụng hết.");
                }

                // Check if user has already used this discount
                bool hasUsed = await HasUserUsedDiscountAsync(userId, discount.Id);
                if (hasUsed)
                {
                    throw new Exception("Bạn đã sử dụng mã giảm giá này rồi.");
                }

                // Record usage
                var discountUsage = new DiscountUsage
                {
                    DiscountId = discount.Id,
                    UserId = userId,
                    UsedAt = now
                };
                
                _db.DiscountUsages.Add(discountUsage);
                _logger.LogInformation($"Added DiscountUsage to context: UserId={userId}, DiscountId={discount.Id}");

                // Decrease usage limit if applicable
                if (discount.UsageLimit.HasValue)
                {
                    var oldLimit = discount.UsageLimit.Value;
                    discount.UsageLimit -= 1;
                    _logger.LogInformation($"Updating UsageLimit for discount {discount.Id}: {oldLimit} -> {discount.UsageLimit}");
                    
                    // Ensure EF tracks the change
                    _db.Entry(discount).Property(d => d.UsageLimit).IsModified = true;
                }

                try
                {
                    // Log what EF will save
                    var entriesToSave = _db.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                        .Select(e => $"{e.Entity.GetType().Name} ({e.State})")
                        .ToList();
                    _logger.LogInformation($"Entities to save: {string.Join(", ", entriesToSave)}");
                    
                    var saveResult = await _db.SaveChangesAsync();
                    _logger.LogInformation($"SaveChangesAsync completed. Affected rows: {saveResult}");
                    
                    if (saveResult == 0)
                    {
                        _logger.LogWarning($"SaveChangesAsync returned 0 affected rows! This might indicate a problem.");
                    }
                    
                    // Only commit if we own the transaction
                    if (ownsTransaction && transaction != null)
                    {
                        await transaction.CommitAsync();
                        _logger.LogInformation($"Transaction committed successfully. User {userId} successfully applied discount code {code}");
                    }
                    else
                    {
                        _logger.LogInformation($"Changes saved (transaction will be committed by caller). User {userId} successfully applied discount code {code}");
                    }
                    
                    // Only verify if we own the transaction (to avoid issues with uncommitted data)
                    if (ownsTransaction && transaction != null)
                    {
                        // Verify the save by querying back
                        var verifyUsage = await _db.DiscountUsages
                            .FirstOrDefaultAsync(u => u.UserId == userId && u.DiscountId == discount.Id);
                        if (verifyUsage != null)
                        {
                            _logger.LogInformation($"✅ Verified: DiscountUsage saved successfully with Id={verifyUsage.Id}");
                        }
                        else
                        {
                            _logger.LogError($"❌ WARNING: DiscountUsage was not found after save! This indicates a problem.");
                        }
                    }
                    
                    return true;
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, $"Database error when saving DiscountUsage. InnerException: {dbEx.InnerException?.Message}");
                    
                    // Check if it's a unique constraint violation
                    if (dbEx.InnerException is SqlException sqlEx)
                    {
                        // Error 2627 = Unique constraint violation
                        // Error 2601 = Duplicate key
                        if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                        {
                            throw new Exception("Bạn đã sử dụng mã giảm giá này rồi.");
                        }
                        // Error 547 = Foreign key constraint violation
                        else if (sqlEx.Number == 547)
                        {
                            throw new Exception("Mã giảm giá không hợp lệ.");
                        }
                        // Error 208 = Invalid object name (table doesn't exist)
                        else if (sqlEx.Number == 208)
                        {
                            throw new Exception("Bảng DiscountUsage chưa được tạo trong database. Vui lòng chạy script SQL để tạo bảng.");
                        }
                    }
                    
                    throw new Exception($"Lỗi khi lưu vào database: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
            }
            catch (Exception ex)
            {
                // Only rollback if we own the transaction
                if (ownsTransaction && transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                _logger.LogError(ex, $"Error applying discount code {code} for user {userId}. Error: {ex.Message}");
                throw;
            }
            finally
            {
                // Only dispose transaction if we own it
                if (ownsTransaction && transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task<Discount> ValidateDiscountAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            var discount = await _db.Discounts
                .FirstOrDefaultAsync(d => d.Code == code && d.IsActive);

            if (discount == null)
                return null;

            var now = DateTime.Now;
            
            // Check date validity
            if (discount.StartAt.HasValue && discount.StartAt > now)
                return null;

            if (discount.EndAt.HasValue && discount.EndAt < now)
                return null;

            // Check usage limit
            if (discount.UsageLimit.HasValue && discount.UsageLimit <= 0)
                return null;

            return discount;
        }

        public async Task<bool> HasUserUsedDiscountAsync(string userId, int discountId)
        {
            return await _db.DiscountUsages
                .AnyAsync(u => u.UserId == userId && u.DiscountId == discountId);
        }

        public async Task<decimal> CalculateDiscountAmountAsync(Discount discount, decimal originalAmount)
        {
            if (discount == null || originalAmount <= 0)
                return 0;

            decimal discountAmount = 0;

            if (discount.Percent > 0)
            {
                discountAmount = originalAmount * discount.Percent / 100;
            }
            else if (discount.Amount.HasValue)
            {
                discountAmount = discount.Amount.Value;
            }

            // Ensure discount doesn't exceed original amount
            return Math.Min(discountAmount, originalAmount);
        }
    }
}
