using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using SellerDashboard.Controllers;
using SellerDashboard.Data;
using SellerDashboard.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class AdminControllerTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            _controller = new AdminController(_context, _mockEnvironment.Object, _mockUserManager.Object);
        }

        [Fact]
        public async Task CreateProduct_ValidProduct_ReturnsJsonSuccess()
        {
            // Arrange
            var product = new Product { Name = "Test Product", Price = 100, StockQuantity = 10 };

            // Act
            var result = await _controller.CreateProduct(product, null);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value.GetType().GetProperty("success").GetValue(jsonResult.Value);
            Assert.True(success);
            Assert.Equal(1, _context.Products.Count());
        }

        [Fact]
        public async Task CreateProduct_ImageTooLarge_ReturnsPartialViewWithError()
        {
            // Arrange
            var product = new Product { Name = "Large Image Product", Price = 100 };
            var fileMock = new Mock<IFormFile>();
            // 8MB > 7MB limit
            fileMock.Setup(_ => _.Length).Returns(8 * 1024 * 1024);
            fileMock.Setup(_ => _.FileName).Returns("large.jpg");

            // Act
            var result = await _controller.CreateProduct(product, fileMock.Object);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_CreateProduct", partialResult.ViewName);
            Assert.True(_controller.ModelState.ContainsKey("imageFile"));
            Assert.Equal("Dung lượng ảnh quá lớn (tối đa 7MB)", _controller.ModelState["imageFile"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task CreateProduct_InvalidImageFormat_ReturnsPartialViewWithError()
        {
            // Arrange
            var product = new Product { Name = "Invalid Format Product", Price = 100 };
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.Length).Returns(1024);
            fileMock.Setup(_ => _.FileName).Returns("test.pdf"); // .pdf not in allowed list

            // Act
            var result = await _controller.CreateProduct(product, fileMock.Object);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("imageFile"));
            Assert.Equal("Định dạng file không hỗ trợ", _controller.ModelState["imageFile"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task CreateProduct_PriceTooLarge_ReturnsPartialViewWithError()
        {
            // Arrange
            var product = new Product { Name = "Expensive Product", Price = 1000000000 }; // > 999,999,999

            // Act
            var result = await _controller.CreateProduct(product, null);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("Price"));
            Assert.Equal("Giá trị quá lớn!", _controller.ModelState["Price"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task CreateProduct_EmptyName_ReturnsPartialViewWithError()
        {
            // Arrange
            var product = new Product { Name = "", Price = 100 };
            _controller.ModelState.AddModelError("Name", "Tên sản phẩm không được để trống");

            // Act
            var result = await _controller.CreateProduct(product, null);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("Name"));
            Assert.Equal("Tên sản phẩm không được để trống", _controller.ModelState["Name"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task CreateProduct_NegativePrice_ReturnsPartialViewWithError()
        {
            // Arrange
            var product = new Product { Name = "Negative Price", Price = -100 };
            _controller.ModelState.AddModelError("Price", "Giá không được nhỏ hơn 0");

            // Act
            var result = await _controller.CreateProduct(product, null);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("Price"));
            Assert.Equal("Giá không được nhỏ hơn 0", _controller.ModelState["Price"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task CreateProduct_StockNotNumber_ReturnsPartialViewWithError()
        {
            // Arrange
            var product = new Product { Name = "Stock chữ", Price = 100 };
            _controller.ModelState.AddModelError("StockQuantity", "Số lượng phải nhập số");

            // Act
            var result = await _controller.CreateProduct(product, null);

            // Assert
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("StockQuantity"));
            Assert.Equal("Số lượng phải nhập số", _controller.ModelState["StockQuantity"].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task UpdateStock_ValidProduct_ReturnsJsonSuccess()
        {
            // Arrange
            var product = new Product { Name = "Stock Product", Price = 10, StockQuantity = 5 };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateStock(product.Id, 20);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value.GetType().GetProperty("success").GetValue(jsonResult.Value);
            Assert.True(success);
            
            var updatedProduct = await _context.Products.FindAsync(product.Id);
            Assert.Equal(20, updatedProduct.StockQuantity);
        }

        [Fact]
        public async Task UpdateStock_ProductNotFound_ReturnsJsonError()
        {
            // Act
            var result = await _controller.UpdateStock(999, 20);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var success = (bool)jsonResult.Value.GetType().GetProperty("success").GetValue(jsonResult.Value);
            var message = (string)jsonResult.Value.GetType().GetProperty("message").GetValue(jsonResult.Value);
            Assert.False(success);
            Assert.Equal("Product not found.", message);
        }
    }
}
