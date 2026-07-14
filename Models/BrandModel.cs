using System.ComponentModel.DataAnnotations;

namespace Xedap.Models
{
    public class BrandModel
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Yêu cầu nhập tên thương hiệu")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Yêu cầu nhập mô tả thương hiệu")]

        public string Description { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Status { get; set; }
    }
}
