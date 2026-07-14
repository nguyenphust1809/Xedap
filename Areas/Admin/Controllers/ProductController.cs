using Xedap.Interfaces;
using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]

    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(DataContext context, IProductService productService, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = context;
            _productService = productService;

            _webHostEnvironment = webHostEnvironment;
        }

        // Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var products = await _dataContext.Products
                .OrderByDescending(p => p.Id)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

            return View(products);
        }

        // GET: Tạo sản phẩm
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name");
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name");
            return View();
        }

        // POST: Tạo sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", product.BrandId);

            if (ModelState.IsValid)
            {
                product.Slug = product.Name.Replace(" ", "-");

                var slugExists = await _dataContext.Products.FirstOrDefaultAsync(p => p.Slug == product.Slug);
                if (slugExists != null)
                {
                    ModelState.AddModelError("", "Sản phẩm đã có trong database");
                    return View(product);
                }

                if (product.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageUpload.CopyToAsync(fs);
                    }

                    product.Image = imageName;
                }

                _dataContext.Add(product);
                await _dataContext.SaveChangesAsync();

                TempData["success"] = "Thêm sản phẩm thành công";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model đang có vài thứ bị lỗi";
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(string.Join("\n", errors));
            }
        }

        // GET: Chỉnh sửa sản phẩm
        public async Task<IActionResult> Edit(int Id)
        {
            var product = await _dataContext.Products.FindAsync(Id);
            if (product == null)
                return NotFound();

            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", product.BrandId);

            return View(product);
        }

        // POST: Chỉnh sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, ProductModel product)
        {
            ViewBag.Categories = new SelectList(_dataContext.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", product.BrandId);

            var existedProduct = await _dataContext.Products.FindAsync(Id);
            if (existedProduct == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                existedProduct.Slug = product.Name.Replace(" ", "-");
                existedProduct.Name = product.Name;
                existedProduct.Description = product.Description;
                existedProduct.CapitalPrice = product.CapitalPrice;

                existedProduct.Price = product.Price;
                existedProduct.CategoryId = product.CategoryId;
                existedProduct.BrandId = product.BrandId;

                if (product.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                    string newFilePath = Path.Combine(uploadsDir, imageName);

                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existedProduct.Image))
                    {
                        string oldFilePath = Path.Combine(uploadsDir, existedProduct.Image);
                        try
                        {
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }
                        catch
                        {
                            ModelState.AddModelError("", "Đã xảy ra lỗi khi xóa ảnh cũ.");
                            return View(product);
                        }
                    }

                    // Upload ảnh mới
                    using (var fs = new FileStream(newFilePath, FileMode.Create))
                    {
                        await product.ImageUpload.CopyToAsync(fs);
                    }

                    existedProduct.Image = imageName;
                }

                _dataContext.Update(existedProduct);
                await _dataContext.SaveChangesAsync();

                TempData["success"] = "Cập nhật sản phẩm thành công";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model đang có vài thứ bị lỗi";
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(string.Join("\n", errors));
            }
        }

        // Xóa sản phẩm
        public async Task<IActionResult> Delete(int Id)
        {
            var product = await _dataContext.Products.FindAsync(Id);
            if (product == null)
                return NotFound();

            if (!string.Equals(product.Image, "noimage.jpg", StringComparison.OrdinalIgnoreCase))
            {
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                string oldFilePath = Path.Combine(uploadsDir, product.Image);

                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }

            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();

            TempData["error"] = "Sản phẩm đã được xóa";
            return RedirectToAction("Index");
        }
        [Route("AddQuantity")]
        [HttpGet]
        public async Task<IActionResult> AddQuantity(int Id)
        {
            var productbyquantity = await _dataContext.ProductQuantities.Where(pq => pq.ProductId == Id).ToListAsync();
            ViewBag.ProductByQuantity = productbyquantity;
            ViewBag.Id = Id;

            return View();
        }
        [Route("StoreProductQuantity")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StoreProductQuantity(ProductQuantityModel productQuantityModel)
        {
            var product = await _dataContext.Products.FindAsync(productQuantityModel.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            // Cập nhật lại số lượng của sản phẩm
            product.Quantity += productQuantityModel.Quantity;

            // Ghi lại lịch sử nhập kho
            productQuantityModel.DateCreated = DateTime.UtcNow;

            _dataContext.Add(productQuantityModel);
            _dataContext.Update(product);
            await _dataContext.SaveChangesAsync(); // <-- thêm await

            TempData["success"] = "Thêm số lượng sản phẩm thành công";
            return RedirectToAction("AddQuantity", "Product", new { Id = productQuantityModel.ProductId });
        }
        [HttpPost]
        public IActionResult ImportExcel(IList<IFormFile> files, int categoryId)
        {
            if (files == null || files.Count == 0)
            {
                TempData["error"] = "Chưa chọn file Excel!";
                return RedirectToAction("Index");
            }

            try
            {
                using var stream = files[0].OpenReadStream();
                using var package = new ExcelPackage(stream);

                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    TempData["error"] = "File Excel không có sheet!";
                    return RedirectToAction("Index");
                }

                int rowCount = ws.Dimension?.End.Row ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    string name = ws.Cells[row, 1].Text.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    if (!int.TryParse(ws.Cells[row, 2].Text.Trim(), out int quantity))
                        continue;

                    string description = ws.Cells[row, 4].Text.Trim();
                    decimal.TryParse(ws.Cells[row, 5].Text.Trim(), out decimal price);
                    decimal.TryParse(ws.Cells[row, 6].Text.Trim(), out decimal capital);
                    string image = ws.Cells[row, 7].Text.Trim();
                    string brandName = ws.Cells[row, 10].Text.Trim();

                    // 🔑 TÌM BRAND (KHÔNG PHÂN BIỆT HOA THƯỜNG)
                    var brand = _dataContext.Brands
                        .FirstOrDefault(b => b.Name.ToLower() == brandName.ToLower());

                    if (brand == null) continue;

                    // 🔍 KIỂM TRA TRÙNG THEO NGHIỆP VỤ
                    var existingProduct = _dataContext.Products.FirstOrDefault(p =>
                        p.Name.ToLower() == name.ToLower() &&
                        p.CategoryId == categoryId &&
                        p.BrandId == brand.Id
                    );

                    if (existingProduct != null)
                    {
                        // ✅ ĐÃ CÓ → CỘNG SỐ LƯỢNG
                        existingProduct.Quantity += quantity;

                        _dataContext.ProductQuantities.Add(new ProductQuantityModel
                        {
                            ProductId = existingProduct.Id,
                            Quantity = quantity,
                            DateCreated = DateTime.UtcNow
                        });

                        _dataContext.Products.Update(existingProduct);
                    }
                    else
                    {
                        // 🆕 CHƯA CÓ → THÊM MỚI
                        _dataContext.Products.Add(new ProductModel
                        {
                            Name = name,
                            Slug = name.ToLower().Replace(" ", "-"),
                            Quantity = quantity,
                            Description = description,
                            Price = price,
                            CapitalPrice = capital,
                            Image = string.IsNullOrEmpty(image) ? "noimage.jpg" : image,
                            CategoryId = categoryId,
                            BrandId = brand.Id
                        });
                    }
                }

                _dataContext.SaveChanges();
                TempData["success"] = "Import thành công – sản phẩm trùng đã được cộng số lượng!";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Lỗi import: " + ex.Message;
            }

            return RedirectToAction("Index");
        }


        public IActionResult DownloadSampleExcel()
        {

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Mẫu Sản Phẩm");

                // Header tiếng Việt
                ws.Cells[1, 1].Value = "Tên sản phẩm";
                ws.Cells[1, 2].Value = "Số lượng";
                ws.Cells[1, 3].Value = "Đã bán";
                ws.Cells[1, 4].Value = "Mô tả";
                ws.Cells[1, 5].Value = "Giá";
                ws.Cells[1, 6].Value = "Giá vốn";
                ws.Cells[1, 7].Value = "Hình ảnh";
                ws.Cells[1, 8].Value = "Slug";
                ws.Cells[1, 9].Value = "Danh mục";
                ws.Cells[1, 10].Value = "Thương hiệu";

                // Dòng mẫu
                ws.Cells[2, 1].Value = "Son môi đỏ";
                ws.Cells[2, 2].Value = 100;
                ws.Cells[2, 3].Value = 0;
                ws.Cells[2, 4].Value = "Son lì cao cấp";
                ws.Cells[2, 5].Value = 250000;
                ws.Cells[2, 6].Value = 150000;
                ws.Cells[2, 7].Value = "son-do.jpg";
                ws.Cells[2, 8].Value = "son-moi-do";
                ws.Cells[2, 9].Value = "Trang điểm";
                ws.Cells[2, 10].Value = "MAC";

                var fileBytes = package.GetAsByteArray();

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "MauSanPham.xlsx"
                );
            }
        }

    }
}
