using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Xedap.Controllers
{
    [Route("[controller]")]
    public class PredictController : Controller
    {
        private readonly string FastApiUrl = "http://localhost:5000/predict";
        private readonly DataContext _context;

        public PredictController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PostPredict(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var client = new HttpClient();
            var form = new MultipartFormDataContent();

            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync(FastApiUrl, form);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(result);

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            var className = root.GetProperty("class").GetString()?.Trim();
            var confidence = root.GetProperty("confidence").GetSingle();

            // 🔹 LẤY SẢN PHẨM LIÊN QUAN THEO DANH MỤC
            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Category.Name.Trim().ToLower() == className.ToLower())
                .Take(4)
                .ToListAsync();

            // 🔹 FALLBACK nếu không tìm thấy sản phẩm liên quan
            if (!relatedProducts.Any())
            {
                relatedProducts = await _context.Products
                    .OrderByDescending(p => p.Brand) // ví dụ: lấy sản phẩm bán chạy
                    .Take(4)
                    .ToListAsync();
            }

            var prediction = new PredictionResult
            {
                ClassName = className,
                Confidence = confidence,
                RelatedProducts = relatedProducts
            };

            return View("Result", prediction);
        }
    }
}
