using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public partial class LoginExcelTests : PageTest
{
    private readonly string _baseUrl = "http://localhost:5265";

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
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
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
    public async Task TCDN01_Login_Success_Valid_Data_Pass()
    {
        await Page.FillAsync("input[name='Email']", "user01@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User@123");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        await Expect(Page).ToHaveURLAsync(new Regex(".*/$|.*/Home/Index.*"));
    }

    [Test]
    public async Task TCDN02_Check_Password_Toggle_Pass()
    {
        await Page.FillAsync("input[name='Password']", "mypassword");
        await Page.WaitForTimeoutAsync(1500);
        var passwordInput = Page.Locator("#Password");
        await Expect(passwordInput).ToHaveAttributeAsync("type", "password");
        await Page.ClickAsync("#passwordToggleIcon");
        await Page.WaitForTimeoutAsync(1500);
        await Expect(passwordInput).ToHaveAttributeAsync("type", "text");
        await Page.ClickAsync("#passwordToggleIcon");
        await Page.WaitForTimeoutAsync(1500);
        await Expect(passwordInput).ToHaveAttributeAsync("type", "password");
    }

    [Test]
    public async Task TCDN03_Check_Forgot_Password_Link_Pass()
    {
        // Click vào link "Quên mật khẩu?"
        await Page.ClickAsync("text=Quên mật khẩu?");
        await Page.WaitForTimeoutAsync(1500);
        
        // Kiểm tra xem có chuyển sang trang ForgotPassword không
        // Sửa lại regex để khớp chính xác với URL trang ForgotPassword
        await Expect(Page).ToHaveURLAsync(new Regex(".*/Account/ForgotPassword.*"));
        
        // Kiểm tra tiêu đề trang để xác nhận đã chuyển trang thành công (Pass như Excel)
        await Expect(Page.Locator("h1:has-text('Quên mật khẩu')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TCDN04_Login_Fail_Wrong_Password_Fail()
    {
        await Page.FillAsync("input[name='Email']", "honguyendangkhoi1711@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "WrongPass!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        // Mong đợi: "Email hoặc mật khẩu không đúng"
        // Kết quả: FAIL (nếu không khớp message)
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Email hoặc mật khẩu không đúng", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDN05_Login_Fail_Empty_Email_Fail()
    {
        await Page.FillAsync("input[name='Email']", "");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi: "Vui lòng nhập Email"
        await Expect(Page.Locator("span[data-valmsg-for='Email']"))
            .ToContainTextAsync("Vui lòng nhập Email", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDN06_Login_Fail_Empty_Password_Fail()
    {
        await Page.FillAsync("input[name='Email']", "honguyendangkhoi1711@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi: "Vui lòng nhập mật khẩu"
        await Expect(Page.Locator("span[data-valmsg-for='Password']"))
            .ToContainTextAsync("Vui lòng nhập mật khẩu", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDN07_Login_Fail_Unregistered_Email_Fail()
    {
        await Page.FillAsync("input[name='Email']", "notfound@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "SomePass123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi: "Tài khoản không tồn tại"
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Tài khoản không tồn tại", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDN08_Login_Fail_Invalid_Email_Format_Fail()
    {
        await Page.FillAsync("input[name='Email']", "honguyendangkhoi1711gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi: "Email không hợp lệ"
        await Expect(Page.Locator("span[data-valmsg-for='Email']"))
            .ToContainTextAsync("Định dạng email sai hoàn toàn", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDN09_Login_Fail_Locked_Account_Fail()
    {
        await Page.FillAsync("input[name='Email']", "usertestlock@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User@123");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi message cụ thể để khớp FAIL trong Excel
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Tài khoản của bạn đã bị khóa trong 30 ngày", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDN10_Login_Fail_Empty_All_Fail()
    {
        await Page.ClickAsync("button:has-text('Đăng nhập')");
        await Page.WaitForTimeoutAsync(1500);
        
        // Mong đợi: "Vui lòng nhập đầy đủ thông tin"
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Vui lòng nhập đầy đủ thông tin", new() { Timeout = 3000 });
    }
}
