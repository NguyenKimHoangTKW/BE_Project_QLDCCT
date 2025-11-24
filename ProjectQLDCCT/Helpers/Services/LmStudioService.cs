using System.Text;
using System.Text.Json;

namespace ProjectQLDCCT.Helpers.Services
{
    public class LmStudioService : ILmStudioService
    {
        private readonly IHttpClientFactory _factory;
        private readonly string _model;

        public LmStudioService(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _model = config["LMStudio:Model"] ?? "fusechat-gemma-2-9b-instruct";
        }

        public async Task<string> SuggestAsync(string sectionTitle, string courseName)
        {
            var client = _factory.CreateClient("LmStudio");

            var prompt = $"Bạn là giảng viên đại học…";

            var payload = new
            {
                model = _model,
                messages = new[]
                {
                new { role = "user", content = prompt }
            }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync("chat/completions", content);

            resp.EnsureSuccessStatusCode();

            var respStr = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respStr);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text ?? "";
        }
    }


}
