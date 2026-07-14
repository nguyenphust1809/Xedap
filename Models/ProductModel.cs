using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xedap.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm bắt buộc nhập")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm bắt buộc nhập")]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        // Đường dẫn ảnh lưu trong DB
        public string? Image { get; set; }

        [NotMapped]
        public IFormFile? ImageUpload { get; set; }

        // Slug tự sinh
        public string? Slug { get; set; }

        // Liên kết danh mục
        [Required(ErrorMessage = "Bạn phải chọn danh mục")]
        public int CategoryId { get; set; }
        public CategoryModel? Category { get; set; }

        // Giá vốn
        [Required(ErrorMessage = "Bạn phải nhập giá vốn")]
        public decimal CapitalPrice { get; set; }

        // Liên kết thương hiệu
        [Required(ErrorMessage = "Bạn phải chọn thương hiệu")]
        public int BrandId { get; set; }
        public BrandModel? Brand { get; set; }

        // Số lượng & đã bán
        public int Quantity { get; set; }
        public int Sold { get; set; }

        // Rating
        public ICollection<RatingModel> Ratings { get; set; } = new List<RatingModel>();
    }
}
