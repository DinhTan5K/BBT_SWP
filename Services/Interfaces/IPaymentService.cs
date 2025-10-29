using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using start.Models;


    public interface IPaymentService
    {
        Task<string> CreatePaymentAsync(decimal amount, string orderInfo, HttpContext httpContext);
        Task<bool> HandleCallbackAsync(IQueryCollection query);
    }

