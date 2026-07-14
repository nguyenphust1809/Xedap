using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Areas.Admin.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Area("Admin")]
    [Route("Admin/Shipping")]
    [Authorize(Roles = "Admin, Publisher, Author")]
    public class ShippingController : Controller
    {
        private readonly DataContext _dataContext;

        public ShippingController(DataContext context)
        {
            _dataContext = context;
        }

        // Quản lý phí shipping
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var shippingList = await _dataContext.Shippings.ToListAsync();
            ViewBag.Shippings = shippingList;
            return View();
        }

        [HttpPost]
        [Route("StoreShipping")]
        public async Task<IActionResult> StoreShipping(ShippingModel shippingModel, string phuong, string quan, string tinh, decimal price)
        {
            shippingModel.City = tinh;
            shippingModel.District = quan;
            shippingModel.Ward = phuong;
            shippingModel.Price = price;

            try
            {
                var exists = await _dataContext.Shippings
                    .AnyAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

                if (exists)
                    return Ok(new { duplicate = true, message = "Dữ liệu trùng lặp." });

                _dataContext.Shippings.Add(shippingModel);
                await _dataContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Thêm shipping thành công" });
            }
            catch
            {
                return StatusCode(500, "Lỗi khi thêm shipping.");
            }
        }

        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var shipping = await _dataContext.Shippings.FindAsync(id);
            if (shipping == null)
            {
                TempData["error"] = "Không tìm thấy shipping cần xóa.";
                return RedirectToAction("Index");
            }

            _dataContext.Shippings.Remove(shipping);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Đã xóa thành công";
            return RedirectToAction("Index");
        }

        // ================== Quản lý hành trình shipper ==================

        // API nhận vị trí từ shipper
        [HttpPost("UpdateLocation")]
        public async Task<IActionResult> UpdateLocation([FromBody] DeliveryTracking tracking)
        {
            if (tracking == null || string.IsNullOrEmpty(tracking.OrderCode) || string.IsNullOrEmpty(tracking.ShipperId))
                return BadRequest("Thông tin không hợp lệ.");

            tracking.TimeStamp = DateTime.UtcNow;
            _dataContext.DeliveryTrackings.Add(tracking);
            await _dataContext.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // Trang xem hành trình của đơn hàng
        [HttpGet("TrackOrder/{orderCode}")]
        public async Task<IActionResult> TrackOrder(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
                return BadRequest("OrderCode không hợp lệ.");

            var trackings = await _dataContext.DeliveryTrackings
                .Where(d => d.OrderCode == orderCode)
                .OrderBy(d => d.TimeStamp)
                .ToListAsync();

            return View(trackings);
        }
    }
}
