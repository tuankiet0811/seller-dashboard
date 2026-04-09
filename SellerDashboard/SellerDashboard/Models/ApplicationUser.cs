using Microsoft.AspNetCore.Identity;

namespace SellerDashboard.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsLocked { get; set; } = false;
    }
}
