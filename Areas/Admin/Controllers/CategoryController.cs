using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Category")]
    [Authorize(Roles = "Admin, Publisher, Author")]
    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;

        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }

        // ================== DANH SÁCH DANH MỤC ==================
        [HttpGet("Index")] // Thêm HttpGet rõ ràng
        public async Task<IActionResult> Index(int pg = 1)
        {
            var category = await _dataContext.Categories.ToListAsync(); // Async tốt hơn

            const int pageSize = 10;
            if (pg < 1) pg = 1;

            int recsCount = category.Count();
            var pager = new Paginate(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;

            var data = category.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;

            return View(data);
        }

        // ================== CREATE DANH MỤC ==================
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryModel category)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Model có lỗi, vui lòng kiểm tra lại";
                return View(category);
            }

            category.Slug = category.Name.Trim().Replace(" ", "-").ToLower();
            bool slugExist = await _dataContext.Categories.AnyAsync(c => c.Slug == category.Slug);

            if (slugExist)
            {
                ModelState.AddModelError("Name", "Danh mục đã tồn tại trong database");
                return View(category);
            }

            _dataContext.Categories.Add(category);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm danh mục thành công";
            return RedirectToAction(nameof(Index));
        }

        // ================== EDIT DANH MỤC ==================
        [HttpGet("Edit/{Id}")]
        public async Task<IActionResult> Edit(int Id)
        {
            var category = await _dataContext.Categories.FindAsync(Id);
            if (category == null)
            {
                TempData["error"] = "Danh mục không tồn tại";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpPost("Edit/{Id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, CategoryModel category)
        {
            if (Id != category.Id)
                return BadRequest("Id không hợp lệ");

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Model có một vài lỗi, vui lòng kiểm tra lại";
                return View(category);
            }

            category.Slug = category.Name.Trim().Replace(" ", "-").ToLower();
            bool slugExist = await _dataContext.Categories
                .AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id);

            if (slugExist)
            {
                ModelState.AddModelError("Name", "Slug đã tồn tại cho danh mục khác");
                return View(category);
            }

            _dataContext.Update(category);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Cập nhật danh mục thành công";
            return RedirectToAction(nameof(Index));
        }

        // ================== DELETE DANH MỤC ==================
        [HttpGet("Delete/{Id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            var category = await _dataContext.Categories.FindAsync(Id);
            if (category == null)
            {
                TempData["error"] = "Danh mục không tồn tại";
                return RedirectToAction(nameof(Index));
            }

            _dataContext.Categories.Remove(category);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Danh mục đã được xóa thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}
