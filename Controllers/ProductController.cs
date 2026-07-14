using Xedap.Models;
using Xedap.Models.ViewModels;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Controllers
{
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        public ProductController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index(int? page, int? categoryId, int? brandId, string search, string sort_by)
        {
            int pageSize = 12;                   // số sản phẩm mỗi trang
            int pageNumber = page ?? 1;          // trang hiện tại

            // Lấy queryable từ DB
            var query = _dataContext.Products
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .AsQueryable();

            // -------------------
            // Filter theo Category / Brand
            // -------------------
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            // -------------------
            // Tìm kiếm không phân biệt chữ hoa/chữ thường
            // -------------------
            if (!string.IsNullOrEmpty(search))
            {
                string searchTerm = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm))
                );
            }

            // -------------------
            // Sort sản phẩm
            // -------------------
            query = sort_by switch
            {
                "price_increase" => query.OrderBy(p => p.Price),
                "price_decrease" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.Id), // hoặc theo ngày tạo nếu có
                _ => query.OrderBy(p => p.Id)
            };

            // -------------------
            // Phân trang
            // -------------------
            int totalItems = await query.CountAsync();  // tổng số sản phẩm sau filter/search
            var products = await query
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            // -------------------
            // Truyền dữ liệu cho View
            // -------------------
            ViewBag.TotalItems = totalItems;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.Search = search;
            ViewBag.SortBy = sort_by;

            return View(products);
        }



        public async Task<IActionResult> Details(int Id)
        {
            if (Id == 0) return RedirectToAction("Index");

            var productsById = await _dataContext.Products
                .Include(p => p.Ratings)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (productsById == null)
                return NotFound();

            var relatedProducts = await _dataContext.Products
                .Where(p => p.CategoryId == productsById.CategoryId && p.Id != productsById.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            var viewModel = new ProductDetailsViewModel
            {
                ProductDetails = productsById,
                Rating = new RatingModel { ProductId = productsById.Id } 
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction("Index");
            }

            var products = await _dataContext.Products
                .Where(p => p.Name.ToLower().Contains(searchTerm.ToLower())
                         || p.Description.ToLower().Contains(searchTerm.ToLower()))
                .ToListAsync();

            ViewBag.Keyword = searchTerm;
            return View(products);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CommentProduct([Bind(Prefix = "Rating")] RatingModel rating)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Error = x.Value.Errors.First().ErrorMessage })
                    .ToList();

                TempData["error"] = string.Join("; ", errors.Select(e => $"{e.Field}: {e.Error}"));
                return RedirectToAction("Details", new { id = rating.ProductId });
            }

            try
            {
                _dataContext.Ratings.Add(rating);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm đánh giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Lỗi khi lưu đánh giá: {ex.Message}";
            }

            return RedirectToAction("Details", new { id = rating.ProductId });
        }

    }
}
