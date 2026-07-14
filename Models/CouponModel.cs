using System.ComponentModel.DataAnnotations;

namespace Xedap.Models
{
    public class CouponModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Yêu cầu tên mã giảm giá")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Yêu cầu mô tả mã giảm giá")]
        public string Description { get; set; }

        public DateTime DateStart { get; set; }
        public DateTime DateExpired { get; set; }

        [Required(ErrorMessage = "Yêu cầu điền số lượng mã giảm giá")]
        public int Quantity { get; set; }

        public int Status { get; set; }

        // Số tiền giảm cố định (VD: 100000 VNĐ)
        public decimal? DiscountAmount { get; set; }

        // Hoặc % giảm giá (VD: 10% -> 10)
        public int? DiscountPercent { get; set; }
    }
}
