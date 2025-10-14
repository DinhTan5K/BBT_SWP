using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using start.Models;

public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public PaymentService(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        

        public async Task<string> CreatePaymentAsync(decimal amountDecimal, string orderInfo, HttpContext httpContext)
        {
            string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
            string partnerCode = _config["Momo:PartnerCode"];
            string accessKey = _config["Momo:AccessKey"];
            string secretKey = _config["Momo:SecretKey"];
            string redirectUrl = _config["Momo:RedirectUrl"];
            string ipnUrl = _config["Momo:IpnUrl"];

            string orderId = Guid.NewGuid().ToString();
            string requestId = Guid.NewGuid().ToString();
            string amount = ((int)amountDecimal).ToString();
            string requestType = "captureWallet";

            string rawHash =
                $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            string signature = CreateSignature(rawHash, secretKey);

            var body = new
            {
                partnerCode,
                partnerName = "Test",
                storeId = "MomoTestStore",
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                lang = "vi",
                extraData = "",
                requestType,
                signature
            };

            var response = await _httpClient.PostAsync(endpoint,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("payUrl").GetString();
        }

    public Task<bool> HandleCallbackAsync(IQueryCollection query)
        {
            var resultCode = query["resultCode"];
            return Task.FromResult(resultCode == "0");
        }

        private string CreateSignature(string rawData, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData)))
                .Replace("-", "")
                .ToLower();
        }
    }
