using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Xedap.Controllers
{
    [Route("shipper")]
    public class ShipperController : Controller
    {
        private readonly DataContext _dataContext;

        public ShipperController(DataContext context)
        {
            _dataContext = context;
        }

        // GET: /shipper
        [HttpGet("")]
        public IActionResult Index()
        {
            // Lấy tất cả đơn hàng đang chờ lấy hàng
            var ordersToDeliver = _dataContext.Orders
                .Where(o => o.Status == 2) // Chỉ hiển thị đơn "Chờ lấy hàng"
                .OrderBy(o => o.CreatedDate)
                .ToList();

            return View(ordersToDeliver);
        }

        // POST: /shipper/MarkInDelivery
        [HttpPost("MarkInDelivery")]
        public IActionResult MarkInDelivery(int id)
        {
            var order = _dataContext.Orders.FirstOrDefault(o => o.Id == id && o.Status == 2);
            if (order != null)
            {
                order.Status = 3; // Chuyển sang "Đang giao"
                _dataContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // POST: /shipper/MarkDelivered
        [HttpPost("MarkDelivered")]
        public IActionResult MarkDelivered(int id)
        {
            var order = _dataContext.Orders.FirstOrDefault(o => o.Id == id && o.Status == 7);
            if (order != null)
            {
                order.Status = 4; // Chuyển sang "Đã giao"
                _dataContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        // GET: /shipper/TrackOrder?orderCode=XYZ
        [HttpGet("TrackOrder")]
        public async Task<IActionResult> TrackOrder(string orderCode)
        {
            var trackings = await _dataContext.DeliveryTrackings
                .Where(d => d.OrderCode == orderCode)
                .OrderBy(d => d.TimeStamp)
                .ToListAsync();

            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            ViewBag.Order = order;
            return View("/Views/Shipping/TrackOrder.cshtml", trackings);
        }


    }

}
