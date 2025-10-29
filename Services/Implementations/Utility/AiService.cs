using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace start.Services
{
    public class AiService
    {
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly HttpClient _httpClient;

        public AiService(IConfiguration configuration)
        {
            _apiKey = configuration["GoogleApiKey"] ?? throw new ArgumentNullException("GoogleApiKey not found in configuration.");
            _modelName = configuration["GeminiModelName"] ?? "gemini-2.5-flash";
            _httpClient = new HttpClient();
        }

        public async Task<string> AskAIAsync(string prompt)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1/models/{_modelName}:generateContent?key={_apiKey}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                // Debug: Log response để kiểm tra
                Console.WriteLine($"API Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    // Thay vì chỉ trả về một thông báo chung, ta trả về chi tiết lỗi
                    return $"Lỗi API: {response.StatusCode}. Chi tiết: {errorBody}";
                }

                dynamic? data = JsonConvert.DeserializeObject(result);
                if (data == null) throw new Exception("Invalid JSON result");


                // Kiểm tra cấu trúc response
                if (data?.candidates != null && data.candidates.Count > 0)
                {
                    var candidate = data.candidates[0];
                    if (candidate?.content?.parts != null && candidate.content.parts.Count > 0)
                    {
                        return candidate.content.parts[0].text ?? "Không có nội dung trong phản hồi.";
                    }
                }

                return $"Cấu trúc phản hồi không đúng: {result}";
            }
            catch (Exception ex)
            {
                return $"Lỗi khi gọi AI: {ex.Message}";
            }
        }
    }
}
