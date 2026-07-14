using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xedap.Models
{
    public class ContactModel
    {
        [Key]
         public int Id { get; set; }  // ✅ Thêm Id làm key

        [Required(ErrorMessage = "Yêu cầu nhập tiêu đề website")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu cầu nhập bản đồ")]
        public string Map { get; set; }
        [Required(ErrorMessage = "Yêu cầu nhập Email")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Yêu cầu nhập phone")]
        public string Phone { get; set; }
        [Required(ErrorMessage =" Yêu cầu nhập thông tin liên hệ")]

        public string Description { get; set; }
        public string? LogoImg { get; set; }
        [NotMapped]
        public IFormFile? ImageUpload { get; set; }
    }
}
