using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Coupon")]
    [Authorize(Roles = "Admin")]
    public class CouponController : Controller
    {
        private readonly DataContext _dataContext;

        public CouponController(DataContext context)
        {
            _dataContext = context;
        }

        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var coupon_list = await _dataContext.Coupons.ToListAsync();
            ViewBag.Coupons = coupon_list;
            return View();
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponModel coupon)
        {
            if (ModelState.IsValid)
            {
                // Thiết lập ngày bắt đầu và ngày hết hạn
                coupon.DateStart = DateTime.UtcNow;
                coupon.DateExpired = coupon.DateExpired == default
                    ? DateTime.UtcNow.AddDays(7)
                    : DateTime.SpecifyKind(coupon.DateExpired, DateTimeKind.Utc);

                // Nếu bạn muốn đảm bảo DiscountAmount và DiscountPercent không null hoặc <0
                coupon.DiscountAmount = coupon.DiscountAmount < 0 ? 0 : coupon.DiscountAmount;
                coupon.DiscountPercent = coupon.DiscountPercent < 0 ? 0 : coupon.DiscountPercent;

                _dataContext.Add(coupon);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm mã giảm giá thành công";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model đang có một vài thứ bị lỗi";

                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                string errorMessage = string.Join("\n ", errors);
                return BadRequest(errorMessage);
            }
        }


    }
}
