using System.ComponentModel.DataAnnotations;

namespace Xedap.Models
{
    public class MomoInfoModel
    {
        [Key]
        public int Id { get; set; }
        public string? OrderId { get; set; }
        public string? OrderInfo { get; set; }
        public string UserName { get; set; }      // Email user

        public string? FullName { get; set; }  
        public decimal Amount { get; set; }
        public DateTime DatePaid { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string Address { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string Province { get; set; }
        public string Note { get; set; }
        public bool IsOrderCreated { get; set; } = false; // đánh dấu đã tạo OrderModel chưa
        public string CartJson { get; set; } = "";  // lưu giỏ hàng dạng JSON
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

    }
}
