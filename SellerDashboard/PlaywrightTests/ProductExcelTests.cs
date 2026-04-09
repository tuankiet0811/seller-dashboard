using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Playwright;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public partial class ProductExcelTests : PageTest
{
    private readonly string _baseUrl = "http://localhost:5265";
    private readonly string _authFile = "admin_auth.json";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            StorageStatePath = _authFile
        };
    }

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // Kiểm tra xem file auth đã tồn tại chưa, nếu chưa thì đăng nhập một lần
        if (!File.Exists(_authFile))
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.GotoAsync($"{_baseUrl}/Account/Login");
            await page.FillAsync("input[name='Email']", "admin1@gmail.com");
            await page.FillAsync("input[name='Password']", "Admin@123");
            await page.ClickAsync("button:has-text('Đăng nhập')");
            
            // Chờ cho đến khi đăng nhập thành công và chuyển hướng
            await page.WaitForURLAsync(new Regex(".*/Admin/Dashboard"));
            
            // Lưu trạng thái đăng nhập (cookies, local storage) vào file
            await context.StorageStateAsync(new() { Path = _authFile });
        }
    }

    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync("about:blank");
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        
        // Không cần gọi LoginAsAdminAsync() nữa vì đã nạp StorageState
        await Page.GotoAsync($"{_baseUrl}/Admin/Dashboard");
        
        // Đi đến trang quản lý sản phẩm
        await Page.ClickAsync("#nav-products");
    }

    [TearDown]
    public async Task TearDown()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        var traceDir = Path.Combine(projectRoot, "TestTraces");
        if (!Directory.Exists(traceDir)) Directory.CreateDirectory(traceDir);

        await Context.Tracing.StopAsync(new()
        {
            Path = Path.Combine(traceDir, $"{testName}.zip")
        });
    }

    [Test]
    public async Task TCTSP01_Add_Product_Success_Pass()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        var productName = $"Product_{Guid.NewGuid()}";
        await Page.FillAsync("input[name='Name']", productName);
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("textarea[name='Description']", "Cotton / Thời trang / Uniqlo");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Category']", "Thời trang");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Brand']", "Uniqlo");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Price']", "200000");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='StockQuantity']", "50");
        await Page.WaitForTimeoutAsync(1500);
        
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator($"td:has-text('{productName}')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TCTSP02_Check_Cancel_Button_Pass()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "Sẽ bị hủy");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Hủy')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("h2:has-text('Sản phẩm')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TCTSP03_Add_Product_Success_No_Description_Pass()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        var productName = $"NoDesc_{Guid.NewGuid()}";
        await Page.FillAsync("input[name='Name']", productName);
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("textarea[name='Description']", "");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Category']", "Điện tử");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Brand']", "Apple");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Price']", "25000000");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='StockQuantity']", "10");
        await Page.WaitForTimeoutAsync(1500);
        
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator($"td:has-text('{productName}')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TCTSP04_Add_Product_Fail_Empty_Name_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Price']", "200000");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='Name']"))
            .ToContainTextAsync("Vui lòng nhập tên sản phẩm", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCTSP05_Add_Product_Fail_Negative_Price_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "Giá âm");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Price']", "-100");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='Price']"))
            .ToContainTextAsync("Giá sản phẩm phải lớn hơn 0", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCTSP06_Add_Product_Fail_Stock_Not_Number_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "Stock chữ");
        await Page.WaitForTimeoutAsync(1500);
        await Page.Locator("input[name='StockQuantity']").FillAsync("abc");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='StockQuantity']"))
            .ToContainTextAsync("Số lượng phải là số nguyên dương", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCTSP07_Add_Product_Fail_Invalid_File_Format_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "File sai");
        await Page.WaitForTimeoutAsync(1500);

        // Tạo file giả lập không hợp lệ (.txt)
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "invalid_file.txt");
        await File.WriteAllTextAsync(filePath, "Đây không phải là ảnh");
        
        // Upload file
        await Page.SetInputFilesAsync("input[name='imageFile']", filePath);
        await Page.WaitForTimeoutAsync(1500);

        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Định dạng file không hỗ trợ", new() { Timeout = 3000 });

        // Xóa file sau khi test
        File.Delete(filePath);
    }

    [Test]
    public async Task TCTSP08_Add_Product_Fail_Empty_Required_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Nhập đầy đủ thông tin bắt buộc", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCTSP09_Add_Product_Fail_Image_Too_Large_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "Ảnh to");
        await Page.WaitForTimeoutAsync(1500);

        // Tạo file ảnh giả lập dung lượng lớn (> 7MB)
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "large_image.jpg");
        byte[] largeData = new byte[8 * 1024 * 1024]; // 8MB
        await File.WriteAllBytesAsync(filePath, largeData);

        // Upload file
        await Page.SetInputFilesAsync("input[name='imageFile']", filePath);
        await Page.WaitForTimeoutAsync(1500);

        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi trong Excel là 5MB, nhưng code thực tế là 7MB -> Sẽ FAIL như mong muốn
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Dung lượng ảnh quá lớn (tối đa 5MB)", new() { Timeout = 3000 });

        // Xóa file sau khi test
        File.Delete(filePath);
    }

    [Test]
    public async Task TCTSP10_Add_Product_Fail_Value_Too_Large_Fail()
    {
        await Page.ClickAsync("button:has-text('Thêm sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Name']", "Giá cực đại");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Price']", "999999999999999");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Lưu sản phẩm')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='Price']"))
            .ToContainTextAsync("Giá trị vượt quá giới hạn cho phép", new() { Timeout = 3000 });
    }
}
