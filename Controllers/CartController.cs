using Xedap.Models;
using Xedap.Models.ViewModels;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc;
using Newtonsoft.Json;

namespace Xedap.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;

        public CartController(DataContext context)
        {
            _dataContext = context;
        }

        public IActionResult Index(ShippingModel shippingModel)
        {
            var cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

            // Lấy giá shipping từ cookie
            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            decimal shippingPrice = 0;
            // Nhận mã giảm
            var coupon_code = Request.Cookies["CouponTitle"];


            if (shippingPriceCookie != null)
            {
                try
                {
                    shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceCookie);
                }
                catch
                {
                    shippingPrice = 0;
                }
            }

            // Lấy coupon code từ cookie
            var couponCode = Request.Cookies["CouponTitle"];

            var cartVM = new CartItemViewModel
            {
                CartItems = cartItems,
                GrandTotal = cartItems.Sum(x => x.Quantity * x.Price),
                ShippingCost = shippingPrice,
                CouponCode = coupon_code
            };

            return View(cartVM);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int id)
        {
            try
            {
                var product = await _dataContext.Products.FindAsync(id);
                if (product == null)
                    return Json(new { success = false, message = "❌ Sản phẩm không tồn tại." });

                // Lấy giỏ hàng từ session
                var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

                // Tìm sản phẩm có sẵn trong giỏ
                var existingItem = cart.FirstOrDefault(c => c.ProductId == id);

                if (existingItem == null)
                    cart.Add(new CartItemModel(product));
                else
                    existingItem.Quantity++;

                // Lưu lại giỏ hàng vào session
                HttpContext.Session.SetJson("Cart", cart);

                // ✅ Tính tổng số lượng và lưu lại vào session để hiển thị ở header
                int totalQuantity = cart.Sum(c => c.Quantity);
                HttpContext.Session.SetInt32("CartCount", totalQuantity);

                return Json(new
                {
                    success = true,
                    message = "✅ Thêm sản phẩm vào giỏ hàng thành công!",
                    cartCount = totalQuantity  // ✅ gửi kèm về client để update ngay
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Lỗi trong Add(): " + ex.Message);
                return StatusCode(500, new { success = false, message = "🔥 Lỗi máy chủ: " + ex.Message });
            }
        }


        public IActionResult Decrease(int id)
        {
            var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            if (cart == null)
            {
                TempData["error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (cartItem == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                return RedirectToAction("Index");
            }

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
            }
            else
            {
                cart.RemoveAll(p => p.ProductId == id);
            }

            if (cart.Count == 0)
                HttpContext.Session.Remove("Cart");
            else
                HttpContext.Session.SetJson("Cart", cart);

            TempData["success"] = "Decrease product quantity successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Increase(int id)
        {
            var product = await _dataContext.Products.FirstOrDefaultAsync(p => p.Id == id);
            var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");

            if (product == null || cart == null)
            {
                TempData["error"] = "Sản phẩm không tồn tại hoặc giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (cartItem == null)
            {
                TempData["error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                return RedirectToAction("Index");
            }

            if (cartItem.Quantity < product.Quantity)
            {
                cartItem.Quantity++;
                TempData["success"] = "Increase product quantity successfully!";
            }
            else
            {
                cartItem.Quantity = product.Quantity;
                TempData["success"] = "Reached maximum available product quantity!";
            }

            HttpContext.Session.SetJson("Cart", cart);
            return RedirectToAction("Index");
        }


        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            if (cart == null)
            {
                TempData["error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            cart.RemoveAll(p => p.ProductId == id);

            if (cart.Count == 0)
                HttpContext.Session.Remove("Cart");
            else
                HttpContext.Session.SetJson("Cart", cart);

            TempData["success"] = "Remove product from cart successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");
            TempData["success"] = "Cleared all products from cart successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> GetShipping(string quan, string tinh, string phuong)
        {
            string cityName = await GetLocationName(1, "0", tinh);
            string districtName = await GetLocationName(2, tinh, quan);
            string wardName = await GetLocationName(3, quan, phuong);

            var existingShipping = await _dataContext.Shippings
                .FirstOrDefaultAsync(x =>
                    x.City == cityName &&
                    x.District == districtName &&
                    x.Ward == wardName);

            decimal shippingPrice = existingShipping?.Price ?? 50000;

            // 🔥 LƯU COOKIE — quan trọng nhất
            Response.Cookies.Append("ShippingPrice", shippingPrice.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(2),
                HttpOnly = false
            });

            return Json(new { shippingPrice });
        }

        private async Task<string> GetLocationName(int type, string parentId, string id)
        {
            using (var client = new HttpClient())
            {
                string url = $"https://esgoo.net/api-tinhthanh/{type}/{parentId}.htm";

                var resp = await client.GetStringAsync(url);
                dynamic data = JsonConvert.DeserializeObject(resp);

                foreach (var item in data.data)
                {
                    if ((string)item.id == id)
                        return (string)item.full_name;
                }

                return null;
            }
        }


        [HttpGet]
        [Route("Cart/DeleteShipping")]
        public IActionResult DeleteShipping()
        {
            Response.Cookies.Delete("ShippingPrice");
            return RedirectToAction("Index","Cart");
        }
        [HttpPost]
        [Route("Cart/GetCoupon")]
        public async Task<IActionResult> GetCoupon(CouponModel couponModel, string coupon_value)
        {
            // 1. Tìm coupon hợp lệ
            var validCoupon = await _dataContext.Coupons
                .FirstOrDefaultAsync(x => x.Name == coupon_value && x.Quantity >= 1);

            if (validCoupon == null)
            {
                return Ok(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết lượt sử dụng." });
            }

            // 2. Tạo tiêu đề coupon
            string couponTitle = validCoupon.Name + "|" + validCoupon.Description;

            // 3. Kiểm tra ngày hết hạn
            TimeSpan remainingTime = validCoupon.DateExpired - DateTime.UtcNow;
            int daysremaining = remainingTime.Days;

            if (daysremaining < 0)
            {
                return Ok(new { success = false, message = "Coupon đã hết hạn." });
            }

            // 4. Áp dụng coupon → set cookie
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };

                Response.Cookies.Append("CouponTitle", couponTitle, cookieOptions);

                return Ok(new { success = true, message = "Áp dụng coupon thành công!", couponTitle });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding apply coupon cookie: {ex.Message}");
                return Ok(new { success = false, message = "Không thể áp dụng coupon." });
            }
        }

    }
}
