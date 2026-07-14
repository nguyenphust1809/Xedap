using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class DashboardController : Controller
    {
        private readonly DataContext _dataContext;

        public DashboardController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        // GET: Admin/Dashboard/Index
        public async Task<IActionResult> Index()
        {
            // Số lượng sản phẩm, đơn hàng đã xử lý, danh mục, người dùng
            var count_product = await _dataContext.Products.CountAsync();
            var count_order = await _dataContext.Orders.CountAsync(o => o.Status == 2); // chỉ tính đã xử lý
            var count_category = await _dataContext.Categories.CountAsync();
            var count_user = await _dataContext.Users.CountAsync();

            ViewBag.CountProduct = count_product;
            ViewBag.CountOrder = count_order;
            ViewBag.CountCategory = count_category;
            ViewBag.CountUser = count_user;

            return View();
        }

        // Lấy toàn bộ dữ liệu thống kê
        [HttpPost]
        public async Task<IActionResult> GetChartData()
        {
            var data = await _dataContext.Statisticials
                .OrderBy(s => s.DateCreated)
                .Select(s => new
                {
                    date = s.DateCreated.ToString("yyyy-MM-dd"),
                    sold = s.Sold,
                    quantity = s.Quantity,
                    revenue = s.Revenue,
                    profit = s.Profit
                })
                .ToListAsync();

            return Json(data);
        }

        // Lấy dữ liệu theo khoảng thời gian
        [HttpPost]
        public async Task<IActionResult> GetChartDateBySelect(string startDate, string endDate)
        {
            try
            {
                if (!DateTime.TryParse(startDate, out DateTime start) ||
                    !DateTime.TryParse(endDate, out DateTime end))
                {
                    return Json(new List<object>());
                }

                // Chuyển sang UTC
                start = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
                end = DateTime.SpecifyKind(end.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                var data = await _dataContext.Statisticials
                    .Where(s => s.DateCreated >= start && s.DateCreated <= end)
                    .OrderBy(s => s.DateCreated)
                    .Select(s => new
                    {
                        date = s.DateCreated.ToString("yyyy-MM-dd"),
                        sold = s.Sold,
                        quantity = s.Quantity,
                        revenue = s.Revenue,
                        profit = s.Profit
                    })
                    .ToListAsync();

                return Json(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetChartDateBySelect: " + ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
