using System.ComponentModel.DataAnnotations;

namespace FoodSystem.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public int Price { get; set; }
        [Required]
        public int? Stock { get; set; }
        public IFormFile? Img { get; set; }
        public string? ImgPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CategoryId { get; set; }
    }

    public class Category
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

    }

    public class Sepet
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public string UrunAd { get; set; }
        public int UrunFiyat { get; set; }
        public int UrunAdet { get; set; }
        public int UserId { get; set; }
    }


        public class ProductCategoryModel
    {
        public List<Product> products { get; set; }
        public List<Category> categories { get; set; }
        public int? SelectCateId { get; set; }
    }

}
