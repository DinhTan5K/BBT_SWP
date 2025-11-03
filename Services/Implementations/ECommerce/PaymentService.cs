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
        string partnerCode = _config["Momo:PartnerCode"] ?? throw new ArgumentNullException("Momo:PartnerCode");
        string accessKey = _config["Momo:AccessKey"] ?? throw new ArgumentNullException("Momo:PartnerCode");
        string secretKey = _config["Momo:SecretKey"] ?? throw new ArgumentNullException("Momo:PartnerCode");
        string redirectUrl = _config["Momo:RedirectUrl"] ?? throw new ArgumentNullException("Momo:PartnerCode");
        string ipnUrl = _config["Momo:IpnUrl"] ?? throw new ArgumentNullException("Momo:PartnerCode");


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
        var payUrl = doc.RootElement.GetProperty("payUrl").GetString();
        if (payUrl == null) throw new Exception("Momo response missing payUrl");
        return payUrl;

    }

    public async Task<(bool success, string? transId)> HandleCallbackAsync(IQueryCollection query)
    {
        var resultCode = query["resultCode"];
        var transId = query["transId"].ToString();
        bool isSuccess = resultCode == "0";
        return (isSuccess, transId);
    }

    private string CreateSignature(string rawData, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData)))
            .Replace("-", "")
            .ToLower();
    }

    public async Task<string> RefundAsync(string transId, decimal amount, string description)
    {
        string endpoint = _config["Momo:RefundEndpoint"]!;
        string partnerCode = _config["Momo:PartnerCode"]!;
        string accessKey = _config["Momo:AccessKey"]!;
        string secretKey = _config["Momo:SecretKey"]!;

        string requestId = Guid.NewGuid().ToString();
        string orderId = $"REFUND_{requestId}";
        long amountLong = (long)amount; // ✅ Ép sang long (MoMo yêu cầu)

        // ✅ Tạo rawHash chuẩn MoMo
        string rawHash = $"accessKey={accessKey}&amount={amountLong}&description={description}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}&transId={transId}";
        string signature = CreateSignature(rawHash, secretKey);

        // ✅ Body đúng định dạng MoMo
        var body = new
        {
            partnerCode,
            accessKey,
            requestId,
            orderId,
            amount = amountLong,              // ✅ là số, không phải chuỗi
            transId = long.Parse(transId),    // ✅ kiểu long
            lang = "vi",
            description,
            signature
        };

        // Log ra cho dễ debug
        Console.WriteLine("Refund Request JSON: " + JsonSerializer.Serialize(body));

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Refund Response: " + result);
        return result;
    }



}
