using System.ComponentModel.DataAnnotations;

namespace SellerDashboard.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email này không hợp lệ. Hãy đảm bảo rằng email được nhập dưới dạng example@email.com")]
        [EmailAddress(ErrorMessage = "Email này không hợp lệ. Hãy đảm bảo rằng email được nhập dưới dạng example@email.com")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất 10 ký tự", MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", 
            ErrorMessage = "Mật khẩu không được chứa dấu cách")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
