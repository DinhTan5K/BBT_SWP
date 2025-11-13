using System.Threading.Tasks;
using start.Models;

namespace start.Services.Interfaces
{
    public interface IDiscountService
    {
        Task<bool> ApplyDiscountAsync(string userId, string code);
        Task<Discount> ValidateDiscountAsync(string code);
        Task<bool> HasUserUsedDiscountAsync(string userId, int discountId);
        Task<decimal> CalculateDiscountAmountAsync(Discount discount, decimal originalAmount);
    }
}
