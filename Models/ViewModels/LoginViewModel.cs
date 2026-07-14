using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Required(ErrorMessage = "Nhập username")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Nhập password")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    [ValidateNever] // <-- Thêm cái này

    public string ReturnUrl { get; set; }
}
