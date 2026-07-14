using Xedap.Interfaces;
using Xedap.Models;
using Xedap.Repository;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xedap.Services
{
    public class ProductService : IProductService
    {
        private readonly DataContext _context;

        public ProductService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Import Excel sản phẩm an toàn, map Brand & Category, tránh duplicate key
        /// </summary>
        /// <param name="filePath">Đường dẫn file Excel</param>
        /// <param name="defaultCategoryId">CategoryId mặc định nếu cột Danh mục rỗng</param>
        public void ImportExcel(string filePath, int defaultCategoryId = 1)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File Excel không tồn tại.", filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null)
                throw new Exception("Excel không có worksheet hợp lệ hoặc rỗng.");

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // Map header: tên cột -> index
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cells[1, col].Text.Trim();
                if (!string.IsNullOrEmpty(header))
                    headerMap[header] = col;
            }

            string GetCell(int row, string colName)
                => headerMap.ContainsKey(colName) ? worksheet.Cells[row, headerMap[colName]].Text.Trim() : "";

            int successCount = 0;
            int skippedCount = 0;

            var existingSlugs = new HashSet<string>();

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var name = GetCell(row, "Tên sản phẩm");
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    // Giá, Giá vốn, Số lượng
                    var price = decimal.TryParse(GetCell(row, "Giá"), out var p) ? p : 0;
                    var capital = decimal.TryParse(GetCell(row, "Giá vốn"), out var c) ? c : 0;
                    var quantity = int.TryParse(GetCell(row, "Số lượng"), out var q) ? q : 0;
                    var description = GetCell(row, "Mô tả");
                    var image = GetCell(row, "Hình ảnh");

                    // Category
                    int categoryId;
                    var categoryName = GetCell(row, "Danh mục");
                    if (!string.IsNullOrWhiteSpace(categoryName))
                    {
                        var category = _context.Categories.FirstOrDefault(c => c.Name == categoryName);
                        if (category == null)
                        {
                            category = new CategoryModel { Name = categoryName };
                            _context.Categories.Add(category);
                            _context.SaveChanges();
                        }
                        categoryId = category.Id;
                    }
                    else
                    {
                        categoryId = defaultCategoryId;
                    }

                    // Brand
                    var brandName = GetCell(row, "Thương hiệu");
                    if (string.IsNullOrWhiteSpace(brandName))
                        throw new Exception("Cột Thương hiệu không được để trống.");

                    var brand = _context.Brands.FirstOrDefault(b => b.Name == brandName);
                    if (brand == null)
                    {
                        brand = new BrandModel { Name = brandName };
                        _context.Brands.Add(brand);
                        _context.SaveChanges();
                    }

                    // Tạo slug duy nhất
                    var slug = GenerateUniqueSlug(name.Replace(" ", "-"), existingSlugs);

                    // Tạo ProductModel
                    var product = new ProductModel
                    {
                        Name = name,
                        Price = price,
                        CapitalPrice = capital,
                        Quantity = quantity,
                        Description = description,
                        Image = image,
                        CategoryId = categoryId,
                        BrandId = brand.Id,
                        Slug = slug
                    };

                    _context.Products.Add(product);
                    successCount++;

                    if (successCount % 50 == 0)
                        _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bỏ qua dòng {row}: {ex.Message}");
                    skippedCount++;
                }
            }

            _context.SaveChanges();
            Console.WriteLine($"Import xong: Thành công {successCount}, Bỏ qua {skippedCount}");
        }

        /// <summary>
        /// SaveChanges wrapper
        /// </summary>
        public void Save()
        {
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                throw new Exception("Lỗi khi lưu dữ liệu vào database: " + ex.InnerException?.Message ?? ex.Message, ex);
            }
        }

        /// <summary>
        /// Sinh slug unique, đảm bảo không trùng trong DB và batch hiện tại
        /// </summary>
        private string GenerateUniqueSlug(string baseSlug, HashSet<string> existingSlugs)
        {
            var slug = baseSlug;
            while (_context.Products.AsNoTracking().Any(p => p.Slug == slug) || existingSlugs.Contains(slug))
            {
                slug = $"{baseSlug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }
            existingSlugs.Add(slug);
            return slug;
        }
    }
}
