using Xedap.Areas.Admin.Repository;
using Xedap.Models;
using Xedap.Models.ViewModels;
using Xedap.Repository;
using Xedap.Services.Momo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Xedap.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CheckoutController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        private readonly IMomoService _momoService;

        public CheckoutController(IEmailSender emailSender,DataContext context,IMomoService momoService)
        {
            _dataContext = context;
            //_emailSender = emailSender;
            _momoService = momoService;

        }

        public async Task<IActionResult> Checkout(string OrderId,
    string ReceiverName,
    string ReceiverPhone,
    string Address,
    string Ward,
    string District,
    string Province,
    string Note,
    string PaymentMethod,
    double? Latitude,
    double? Longitude)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {

                var ordercode = Guid.NewGuid().ToString();
                var orderItem = new OrderModel();
                orderItem.OrderCode = ordercode;
                var shippingPriceCookie = Request.Cookies["ShippingPrice"];
                decimal shippingPrice = 0;
                var coupon_code = Request.Cookies["CouponTitle"];

                if(shippingPriceCookie != null)
                {
                    var shippingPriceJson = shippingPriceCookie;
                    shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
                }
                else
                {
                    shippingPrice = 0;
                }
                    orderItem.ShippingCost = shippingPrice;
                orderItem.CouponCode = coupon_code;

                orderItem.UserName = userEmail;
                if (!string.IsNullOrEmpty(OrderId))
                {
                    // Thanh toán online (MOMO)
                    orderItem.PaymentMethod = "MOMO";
                    orderItem.Status = 2; // Trạng thái "Chờ lấy hàng"
                }
                else
                {
                    // COD
                    orderItem.PaymentMethod = "COD";
                    orderItem.Status = 1; // Trạng thái "Mới tạo"
                }

                orderItem.CreatedDate = DateTime.UtcNow;
                orderItem.ReceiverName = ReceiverName;
                orderItem.ReceiverPhone = ReceiverPhone;
                orderItem.Address = Address;
                orderItem.Ward = Ward;
                orderItem.District = District;
                orderItem.Province = Province;
                orderItem.Latitude = Latitude;
                orderItem.Longitude = Longitude;

                //Nhận coupon code
                var CouponCode = Request.Cookies["CouponTitle"];
                _dataContext.Add(orderItem);
                _dataContext.SaveChanges();
                //tạo order detail
                List<CartItemModel> cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
                foreach (var cart in cartItems)
                {
                    var orderdetail = new OrderDetails();
                    orderdetail.UserName = userEmail;
                    orderdetail.OrderCode = ordercode;

                    orderdetail.ProductId = cart.ProductId;
                    orderdetail.Price = cart.Price;
                    orderdetail.Quantity = cart.Quantity;
                    //update product quantity
                    var product = await _dataContext.Products.Where(p => p.Id == cart.ProductId).FirstAsync();
                    product.Quantity -= cart.Quantity;
                    product.Sold += cart.Quantity;
                    _dataContext.Update(product);
                    _dataContext.Add(orderdetail);
                    _dataContext.SaveChanges();

                }
                HttpContext.Session.Remove("Cart");
                //Send mail order when success
                var receiver = userEmail;
                var subject = "Đặt hàng thành công";
                var message = "Đặt hàng thành công, trải nghiệm nhé.";

                //await _emailSender.SendEmailAsync(receiver, subject, message);

                TempData["success"] = "Đơn hàng đã được tạo,vui lòng chờ duyệt đơn hàng nhé.";
                return RedirectToAction("History", "Account");
            }
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack(string orderId, string resultCode)
        {
            // 1️⃣ Lấy thông tin giao dịch MOMO
            var momoInfo = await _dataContext.MomoInfos.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (momoInfo == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng MOMO";
                return RedirectToAction("History", "Account");
            }

            // 2️⃣ Kiểm tra kết quả thanh toán
            if (resultCode != "0")
            {
                TempData["error"] = "Giao dịch MOMO không thành công";
                return RedirectToAction("History", "Account");
            }

            // 3️⃣ Nếu chưa tạo OrderModel thì tạo
            if (!momoInfo.IsOrderCreated)
            {
                var orderCode = Guid.NewGuid().ToString();
                var orderItem = new OrderModel
                {
                    OrderCode = orderCode,
                    UserName = momoInfo.UserName,
                    PaymentMethod = "MOMO",
                    Status = 2,
                    CreatedDate = DateTime.UtcNow,
                    ReceiverName = momoInfo.ReceiverName,
                    ReceiverPhone = momoInfo.ReceiverPhone,
                    Address = momoInfo.Address,
                    Ward = momoInfo.Ward,
                    District = momoInfo.District,
                    Province = momoInfo.Province,
                    ShippingCost = Request.Cookies["ShippingPrice"] != null
                        ? JsonConvert.DeserializeObject<decimal>(Request.Cookies["ShippingPrice"])
                        : 0,
                    CouponCode = Request.Cookies["CouponTitle"]
                };
                _dataContext.Add(orderItem);

                var cartItems = JsonConvert.DeserializeObject<List<CartItemModel>>(momoInfo.CartJson);
                foreach (var cart in cartItems)
                {
                    var orderDetail = new OrderDetails
                    {
                        UserName = momoInfo.UserName,
                        OrderCode = orderCode,
                        ProductId = cart.ProductId,
                        Price = cart.Price,
                        Quantity = cart.Quantity
                    };

                    var product = await _dataContext.Products.FirstAsync(p => p.Id == cart.ProductId);
                    product.Quantity -= cart.Quantity;
                    product.Sold += cart.Quantity;

                    _dataContext.Update(product);
                    _dataContext.Add(orderDetail);
                }

                momoInfo.IsOrderCreated = true;
                _dataContext.Update(momoInfo);

                await _dataContext.SaveChangesAsync();
            }

            // 4️⃣ Xoá giỏ hàng session
            HttpContext.Session.Remove("Cart");

            TempData["success"] = "Đặt hàng thành công qua MOMO!";
            return RedirectToAction("History", "Account");
        }

        public IActionResult Index()
        {
            var cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

            var model = new CartItemViewModel
            {
                CartItems = cartItems,
                GrandTotal = cartItems.Sum(c => c.Price * c.Quantity),
                ShippingCost = 0,  // Chưa tính phí ship
                CouponCode = null  // Chưa áp dụng coupon
            };

            return View(model);
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
            return RedirectToAction("Index", "Cart");
        }
        [HttpPost]
        [Route("Checkout/GetCoupon")]
        public async Task<IActionResult> GetCoupon(string coupon_value)
        {
            // 1. Tìm coupon hợp lệ
            var validCoupon = await _dataContext.Coupons
                .FirstOrDefaultAsync(x => x.Name == coupon_value && x.Quantity >= 1);

            if (validCoupon == null)
            {
                return Ok(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết lượt sử dụng." });
            }

            // 2. Kiểm tra ngày hết hạn
            if (validCoupon.DateExpired < DateTime.UtcNow)
            {
                return Ok(new { success = false, message = "Coupon đã hết hạn." });
            }

            // 3. Tạo tiêu đề coupon
            string couponTitle = validCoupon.Name + "|" + validCoupon.Description;

            // 4. Tính tổng tiền hiện tại từ giỏ hàng
            var cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            decimal totalPrice = cartItems.Sum(c => c.Price * c.Quantity);

            // 5. Tính số tiền giảm
            decimal discount = 0;
            if (validCoupon.DiscountAmount.HasValue)
                discount = validCoupon.DiscountAmount.Value;
            else if (validCoupon.DiscountPercent.HasValue)
                discount = totalPrice * validCoupon.DiscountPercent.Value / 100;

            // 6. Lưu coupon vào cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("CouponTitle", couponTitle, cookieOptions);

            // 7. Trả về thông tin
            return Ok(new
            {
                success = true,
                message = "Áp dụng coupon thành công!",
                couponTitle,
                discount,
                totalAfterDiscount = totalPrice - discount
            });
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmCheckout(MomoInfoModel model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
                return RedirectToAction("Login", "Account");

            var cartItems = !string.IsNullOrEmpty(model.CartJson)
                ? JsonConvert.DeserializeObject<List<CartItemModel>>(model.CartJson)
                : HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // Tạo OrderModel
            var orderCode = Guid.NewGuid().ToString();
            var order = new OrderModel
            {
                OrderCode = orderCode,
                UserName = userEmail,
                ReceiverName = model.ReceiverName,
                ReceiverPhone = model.ReceiverPhone,
                Address = model.Address,
                Ward = model.Ward,
                District = model.District,
                Province = model.Province,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                PaymentMethod = "MOMO",
                Status = 2,
                ShippingCost = Request.Cookies["ShippingPrice"] != null
                    ? JsonConvert.DeserializeObject<decimal>(Request.Cookies["ShippingPrice"])
                    : 0,
                CreatedDate = DateTime.UtcNow,
                CouponCode = Request.Cookies["CouponTitle"]
            };
            _dataContext.Add(order);

            // Tạo OrderDetails & cập nhật sản phẩm
            foreach (var cart in cartItems)
            {
                var detail = new OrderDetails
                {
                    OrderCode = orderCode,
                    UserName = userEmail,
                    ProductId = cart.ProductId,
                    Price = cart.Price,
                    Quantity = cart.Quantity
                };
                _dataContext.Add(detail);

                var product = await _dataContext.Products.FirstAsync(p => p.Id == cart.ProductId);
                product.Quantity -= cart.Quantity;
                product.Sold += cart.Quantity;
                _dataContext.Update(product);
            }

            // Lưu MomoInfoModel
            model.OrderId = orderCode;
            model.UserName = userEmail;
            model.FullName = model.ReceiverName;
            model.CartJson = JsonConvert.SerializeObject(cartItems);
            decimal shippingCost = Request.Cookies["ShippingPrice"] != null
                ? JsonConvert.DeserializeObject<decimal>(Request.Cookies["ShippingPrice"])
                : 0;

            decimal discount = 0;
            var couponCookie = Request.Cookies["CouponTitle"];
            if (!string.IsNullOrEmpty(couponCookie))
            {
                // Giả sử bạn parse discount từ cookie hoặc tính lại
                // vd: coupon format = "CODE|Mô tả|50" -> 50 là tiền giảm
                var parts = couponCookie.Split('|');
                if (parts.Length > 2 && decimal.TryParse(parts[2], out var parsedDiscount))
                    discount = parsedDiscount;
            }

            model.Amount = cartItems.Sum(c => c.Price * c.Quantity) + shippingCost - discount;
            model.IsOrderCreated = false;

            _dataContext.Add(model);
            await _dataContext.SaveChangesAsync();

            // --- Tạo OrderInfoModel từ MomoInfoModel để gửi MOMO ---
            var orderInfo = new OrderInfoModel
            {
                FullName = model.FullName,
                OrderId = model.OrderId,
                OrderInfo = string.Join(", ", cartItems.Select(c => $"{c.ProductName} x{c.Quantity}")),
                Amount = (double)model.Amount,
                ReceiverName = model.ReceiverName,
                ReceiverPhone = model.ReceiverPhone,
                Address = model.Address,
                Ward = model.Ward,
                District = model.District,
                Province = model.Province,
                Note = model.Note
            };

            var momoResponse = await _momoService.CreatePaymentMomo(orderInfo);
            return Redirect(momoResponse.PayUrl);
        }


    }
}