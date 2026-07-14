using Xedap.Models;
using Xedap.Models.ViewModels;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Order")]
    [Authorize(Roles = "Publisher,Author,Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        private const int PageSize = 10; // số lượng đơn hàng trên 1 trang

        public OrderController(DataContext context)
        {
            _dataContext = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(
     string searchOrderCode,
     string searchUserName,
     int? status,
     int page = 1)
        {
            const int PageSize = 10; // số đơn trên 1 trang

            // 1. Query gốc
            var query = _dataContext.Orders.AsQueryable();

            // 2. Filter
            if (!string.IsNullOrEmpty(searchOrderCode))
                query = query.Where(o => o.OrderCode.Contains(searchOrderCode));

            if (!string.IsNullOrEmpty(searchUserName))
                query = query.Where(o => o.UserName.Contains(searchUserName));

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            // 3. Sort giảm dần theo ngày tạo
            query = query.OrderByDescending(o => o.CreatedDate);

            // 4. Tổng số bản ghi
            var totalItems = await query.CountAsync();

            // 5. Phân trang
            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // 6. Gửi ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            ViewBag.SearchOrderCode = searchOrderCode;
            ViewBag.SearchUserName = searchUserName;
            ViewBag.StatusFilter = status;

            return View(items);
        }


        [HttpGet]
        [Route("ViewOrder")]
        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if (order == null) return NotFound();

            var orderItems = await _dataContext.OrdersDetails
                            .Where(od => od.OrderCode == ordercode)
                .Include(od => od.Product)
                .ToListAsync();

            var model = new OrderDetailViewModel
            {
                OrderCode = order.OrderCode,
                Status = order.Status,
                ShippingCost = order.ShippingCost,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                Address = order.Address,
                Ward = order.Ward,
                District = order.District,
                Province = order.Province,
                Items = orderItems
            };

            return View(model);
        }

        [HttpGet]
        [Route("PaymentMomoInfo")]
        public async Task<IActionResult> PaymentMomoInfo(string orderId)
        {
            var momoInfo = await _dataContext.MomoInfos.FirstOrDefaultAsync(m => m.OrderId == orderId);
            if(momoInfo == null)
            {
                return NotFound();
            }
            return View (momoInfo);
        }
        [HttpPost]
        [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            if (string.IsNullOrEmpty(ordercode))
                return Json(new { success = false, message = "Không tìm thấy mã đơn hàng" });

            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if (order == null)
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });

            order.Status = status;
            await _dataContext.SaveChangesAsync();  // lưu trạng thái đơn hàng trước

            // ==============================================
            // 🔥 CHỈ CẬP NHẬT THỐNG KÊ KHI ĐƠN ĐÃ XỬ LÝ (status = 2)
            // ==============================================
            if (status == 2)
            {
                // lấy chi tiết đơn
                var details = await _dataContext.OrdersDetails
                    .Include(d => d.Product)
                    .Where(d => d.OrderCode == ordercode)
                    .ToListAsync();

                var date = order.CreatedDate.Date;

                var stat = await _dataContext.Statisticials
                    .FirstOrDefaultAsync(s => s.DateCreated.Date == date);

                if (stat != null)
                {
                    // cập nhật bản ghi thống kê
                    stat.Quantity += details.Sum(x => x.Quantity);
                    stat.Sold += details.Sum(x => x.Quantity);
                    stat.Revenue += details.Sum(x => x.Quantity * x.Product.Price);
                    stat.Profit += details.Sum(x => x.Quantity * (x.Product.Price - x.Product.CapitalPrice));

                    _dataContext.Statisticials.Update(stat);
                }
                else
                {
                    // tạo mới thống kê
                    var newStat = new StatisticialModel
                    {
                        DateCreated = date,
                        Quantity = details.Sum(x => x.Quantity),
                        Sold = details.Sum(x => x.Quantity),
                        Revenue = details.Sum(x => x.Quantity * x.Product.Price),
                        Profit = details.Sum(x => x.Quantity * (x.Product.Price - x.Product.CapitalPrice))
                    };

                    await _dataContext.Statisticials.AddAsync(newStat);
                }

                await _dataContext.SaveChangesAsync();   // lưu thống kê
            }

            return Json(new { success = true, message = "Cập nhật thành công" });
        }

        [HttpGet]
                    [Route("Delete")]
                    public async Task<IActionResult> Delete(string ordercode)
                    {
                        var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);

                        if (order == null)
                        {
                            return NotFound();
                        }
                        try
                        {

                            //delete order
                            _dataContext.Orders.Remove(order);


                            await _dataContext.SaveChangesAsync();

                            return RedirectToAction("Index");
                        }
                        catch (Exception)
                        {

                            return StatusCode(500, "An error occurred while deleting the order.");
                        }
                    }

    }
}