using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using SellerDashboard.Controllers;
using SellerDashboard.Data;
using SellerDashboard.Models;
using SellerDashboard.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManager = new Mock<SignInManager<ApplicationUser>>(_userManager.Object, contextAccessor.Object, userClaimsPrincipalFactory.Object, null, null, null, null);

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _controller = new AccountController(_userManager.Object, _signInManager.Object, _roleManager.Object, _context);
            
            // Mock HttpContext for Session and other things if needed
            var httpContext = new DefaultHttpContext();
            var sessionMock = new Mock<ISession>();
            httpContext.Session = sessionMock.Object;
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        }

        [Fact]
        public async Task Login_ValidUser_ReturnsRedirect()
        {
            // Arrange
            var model = new LoginViewModel { Email = "user01@gmail.com", Password = "User@123" };
            var user = new ApplicationUser { Email = model.Email, IsLocked = false };

            _userManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _signInManager.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManager.Setup(x => x.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_LockedUser_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel { Email = "locked@gmail.com", Password = "User@123" };
            var user = new ApplicationUser { Email = model.Email, IsLocked = true };

            _userManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Tài khoản không khả dụng trong 30 ngày", _controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel { Email = "honguyendangkhoi1711@gmail.com", Password = "WrongPass!" };
            var user = new ApplicationUser { Email = model.Email };

            _userManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _signInManager.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Mật khẩu không đúng!", _controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Login_UnregisteredEmail_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel { Email = "notfound@gmail.com", Password = "SomePass123!" };

            _userManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Email không tồn tài!", _controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Login_EmptyAll_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel { Email = "", Password = "" };
            _controller.ModelState.AddModelError("Email", "Email không đúng!");
            _controller.ModelState.AddModelError("Password", "Mật khẩu không đúng!");

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Không được để trống các trường", _controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsRedirect()
        {
            // Arrange
            var model = new RegisterViewModel { Email = "new@gmail.com", Password = "User123456!", ConfirmPassword = "User123456!" };
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);
            _roleManager.Setup(x => x.RoleExistsAsync("Customer")).ReturnsAsync(true);
            _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Register(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsViewWithError()
        {
            // Arrange
            var model = new RegisterViewModel { Email = "admin1@gmail.com", Password = "User123456!", ConfirmPassword = "User123456!" };
            var errors = new List<IdentityError> { new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" } };
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains("Địa chỉ email này đã được liên kết với một tài khoản hiện có. Để tiếp tục, hãy đăng nhập", _controller.ModelState[string.Empty].Errors.Select(e => e.ErrorMessage));
        }

        [Fact]
        public async Task Register_EmptyAll_ReturnsViewWithError()
        {
            // Arrange
            var model = new RegisterViewModel { Email = "", Password = "", ConfirmPassword = "" };
            _controller.ModelState.AddModelError("Email", "Email này không hợp lệ. Hãy đảm bảo rằng email được nhập dưới dạng example@email.com");
            _controller.ModelState.AddModelError("Password", "Mật khẩu không được để trống");

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Không được để trống các trường", _controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ForgotPassword_ValidUser_ReturnsRedirect()
        {
            // Arrange
            var model = new ForgotPasswordViewModel { Email = "user01@gmail.com", NewPassword = "NewPassword123!", ConfirmPassword = "NewPassword123!" };
            var user = new ApplicationUser { Email = model.Email };

            _userManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.AddPasswordAsync(user, model.NewPassword)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Đổi mật khẩu thành công! Hãy đăng nhập lại.", _controller.TempData["SuccessMessage"]);
        }
    }
}
