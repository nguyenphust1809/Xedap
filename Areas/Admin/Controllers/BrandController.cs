using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Brand")]
    [Authorize(Roles = "Admin, Publisher, Author")]
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;

        public BrandController(DataContext context)
        {
            _dataContext = context;
        }

        // GET: Admin/Brand/Index
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int pg = 1)
        {
            List<BrandModel> brand = await _dataContext.Brands.ToListAsync();

            const int pageSize = 10;
            if (pg < 1) pg = 1;
            int recsCount = brand.Count();
            var pager = new Paginate(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = brand.Skip(recSkip).Take(pager.PageSize).ToList();

            ViewBag.Pager = pager;
            return View(data);
        }

        // GET: Admin/Brand/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Brand/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandModel brand)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            brand.Slug = brand.Name.Replace(" ", "-");
            var slugExists = await _dataContext.Brands.AnyAsync(p => p.Slug == brand.Slug);
            if (slugExists)
            {
                ModelState.AddModelError("", "Danh mục đã có trong database");
                return View(brand);
            }

            _dataContext.Add(brand);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Thêm thương hiệu thành công";
            return RedirectToAction("Index");
        }

        // GET: Admin/Brand/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

       [HttpPost("Edit/{id}")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, BrandModel brand)
{
    if (id != brand.Id) return BadRequest();
    if (!ModelState.IsValid) return View(brand);

    var existingBrand = await _dataContext.Brands.FindAsync(id);
    if (existingBrand == null) return NotFound();

    // Cập nhật từng field thủ công
    existingBrand.Name = brand.Name;
    existingBrand.Description = brand.Description;
    existingBrand.Status = brand.Status;  // ✅ đảm bảo Status được lưu
    existingBrand.Slug = brand.Name.Replace(" ", "-");

    await _dataContext.SaveChangesAsync();

    TempData["success"] = "Cập nhật thương hiệu thành công";
    return RedirectToAction("Index");
}

        // GET: Admin/Brand/Delete/5
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand); // hiển thị confirm delete
        }

        // POST: Admin/Brand/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            _dataContext.Brands.Remove(brand);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thương hiệu đã được xóa thành công";
            return RedirectToAction("Index");
        }
    }
}
