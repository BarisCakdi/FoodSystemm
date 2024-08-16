using System.ComponentModel.DataAnnotations;

namespace FoodSystem.Models
{
    public class Register
    {
        public int Id { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string RepeatPass { get; set; }
        [Required]
        public string Email { get; set; }
        [Required] 
        public int PhoneNumber { get; set; }
        [Required]
        public string Address { get; set; }
        public IFormFile? Img {  get; set; }
        public string? ImgPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? RedirectUrl { get; set; }
    }
}
