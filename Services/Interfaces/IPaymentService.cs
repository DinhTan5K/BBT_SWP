using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using start.Models;


    public interface IPaymentService
    {
        Task<string> CreatePaymentAsync(decimal amount, string orderInfo, HttpContext httpContext);
    Task<(bool success, string? transId)> HandleCallbackAsync(IQueryCollection query);
         Task<string> RefundAsync(string transId, decimal amount, string description);
    }

