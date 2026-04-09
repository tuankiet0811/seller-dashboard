using System.ComponentModel.DataAnnotations;

namespace SellerDashboard.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email không đúng!")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không đúng!")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
