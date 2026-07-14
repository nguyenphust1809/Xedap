using NetTopologySuite.Geometries; // 👈 thêm dòng này

namespace Xedap.Models
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Point? Location { get; set; } // ✅ Cột PostGIS geometry(Point, 4326)
        public string? ImageUrl { get; set; }
        public string Overview { get; set; }   // tóm tắt nhanh
        public string Description { get; set; } // mô tả chi tiết
        public double Rating { get; set; } = 5; // điểm đánh giá (tùy chọn)

    }
}
