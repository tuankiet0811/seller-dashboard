using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SellerDashboard.Data;
using SellerDashboard.Extensions;
using SellerDashboard.Models;
using SellerDashboard.ViewModels;

namespace SellerDashboard.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        private async Task MergeSessionCartToDbAsync(ApplicationUser user)
        {
            var sessionCart = HttpContext.Session.Get<List<CartItem>>("Cart");
            if (sessionCart != null && sessionCart.Any())
            {
                var dbCart = await _context.ShoppingCarts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (dbCart == null)
                {
                    dbCart = new ShoppingCart { UserId = user.Id };
                    _context.ShoppingCarts.Add(dbCart);
                }

                foreach (var sessionItem in sessionCart)
                {
                    var existingItem = dbCart.Items.FirstOrDefault(i => i.ProductId == sessionItem.ProductId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += sessionItem.Quantity;
                    }
                    else
                    {
                        dbCart.Items.Add(new ShoppingCartItem
                        {
                            ProductId = sessionItem.ProductId,
                            Quantity = sessionItem.Quantity
                        });
                    }
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("Cart");
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)                    
        {
            if (ModelState.IsValid)                                                             //1
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };    //2
                var result = await _userManager.CreateAsync(user, model.Password);              //3

                if (result.Succeeded)                                                           //4
                {
                    // Tự động gán role Customer
                    if (!await _roleManager.RoleExistsAsync("Customer"))                        //5
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));           //6
                    }
                    
                    await _userManager.AddToRoleAsync(user, "Customer");                        //7

                    await _signInManager.SignInAsync(user, isPersistent: false);                //8
                    await MergeSessionCartToDbAsync(user);                                      //9

                    return RedirectToAction("Index", "Home");                                   //10
                }

                foreach (var error in result.Errors)                                            //11
                {
                    ModelState.AddModelError(string.Empty, error.Description);                  //12
                }

                if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
                {
                    ModelState.AddModelError(string.Empty, "Địa chỉ email này đã được liên kết với một tài khoản hiện có. Để tiếp tục, hãy đăng nhập");
                }
            }
            
            if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Không được để trống các trường");
            }
            
            return View(model);                                                                 //13
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)                                    
        {
            if (ModelState.IsValid)                                                                                                             //1
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email không tồn tài!");
                    return View(model);
                }

                if (user.IsLocked)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản không khả dụng trong 30 ngày");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);  //2
                if (result.Succeeded)                                                                                                           //3
                {
                    await MergeSessionCartToDbAsync(user);                                                                                  //6
                    
                    if (await _userManager.IsInRoleAsync(user, "Admin"))                                                                    //7
                    {
                        return RedirectToAction("Dashboard", "Admin");                                                                      //8
                    }
                    return RedirectToAction("Index", "Home");                                                                                   //9
                }
                ModelState.AddModelError(string.Empty, "Mật khẩu không đúng!");                                                          //10
            }
            
            if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Không được để trống các trường");
            }
            
            return View(model);                                                                                                                 //11
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại");
                    return View(model);
                }

                // Simplified password reset for this request
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (removeResult.Succeeded)
                {
                    var addResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    if (addResult.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Hãy đăng nhập lại.";
                        return RedirectToAction("Login");
                    }
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }
        
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
