using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Xedap.Controllers
{
    

    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;
        }
      public async Task<IActionResult> Index(string Slug = "")
{
    BrandModel brand = _dataContext.Brands
        .Where(p => p.Slug == Slug)
        .FirstOrDefault();
        
    if (brand == null) return RedirectToAction("Index");

    var productsByBrand = _dataContext.Products
        .Include(p => p.Category)  // ✅ thêm dòng này
        .Include(p => p.Brand)     // ✅ thêm dòng này
        .Where(p => p.BrandId == brand.Id);

    return View(await productsByBrand.OrderByDescending(p => p.Id).ToListAsync());
}
    }   
}
