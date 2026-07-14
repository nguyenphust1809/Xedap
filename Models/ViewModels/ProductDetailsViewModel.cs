using System.ComponentModel.DataAnnotations;

namespace Xedap.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public ProductModel ProductDetails { get; set; }

        public RatingModel Rating { get; set; } = new RatingModel(); 
    }
}
