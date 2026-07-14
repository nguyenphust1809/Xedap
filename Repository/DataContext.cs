using Xedap.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Repository
{
    public class DataContext : IdentityDbContext<AppUserModel>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
         
        public DbSet<BrandModel> Brands { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<RatingModel> Ratings { get; set; }

        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderDetails> OrdersDetails { get; set; }

        public DbSet<ContactModel> Contact { get; set; }
        public DbSet<WishlistModel> Wishlist { get; set; }
        public DbSet<CompareModel> Compare { get; set; }
        public DbSet<ProductQuantityModel> ProductQuantities { get; set; }

        public DbSet<ShippingModel> Shippings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<CouponModel> Coupons { get; set; }
        public DbSet<StatisticialModel> Statisticials { get; set; }
        public DbSet<MomoInfoModel> MomoInfos { get; set; }
        public DbSet<DeliveryTracking> DeliveryTrackings { get; set; }



    }
}
