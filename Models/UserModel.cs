using System.ComponentModel.DataAnnotations;

namespace Xedap.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "nhập username")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "nhập password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
    }
}
