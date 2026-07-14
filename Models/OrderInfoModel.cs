namespace Xedap.Models
{
    public class OrderInfoModel
    {
        public string FullName { get; set; }
        public string OrderId { get; set; } 
        public string OrderInfo { get; set; }
        public double Amount { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string Address { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string Province { get; set; }
        public string Note { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
