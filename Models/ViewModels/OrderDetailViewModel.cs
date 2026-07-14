namespace Xedap.Models.ViewModels
{
    public class OrderDetailViewModel
    {
        public string OrderCode { get; set; }
        public int Status { get; set; }
        public decimal ShippingCost { get; set; }

        // Thông tin người nhận
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string Address { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string Province { get; set; }
        public string Note { get; set; }

        public List<OrderDetails> Items { get; set; }
    }

}
