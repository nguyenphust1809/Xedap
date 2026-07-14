using System;
using System.ComponentModel.DataAnnotations;

namespace Xedap.Models
{
    public class DeliveryTracking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OrderCode { get; set; }   // Mã đơn hàng

        [Required]
        public string ShipperId { get; set; }   // Id shipper

        public string ShipperName { get; set; } // Tên shipper

        [Required]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; }      // Chờ lấy hàng / Đang giao / Đã giao

        public double? Latitude { get; set; }   // Vĩ độ vị trí
        public double? Longitude { get; set; }  // Kinh độ vị trí

        public string Note { get; set; }        // Ghi chú (tùy chọn)
    }
}
