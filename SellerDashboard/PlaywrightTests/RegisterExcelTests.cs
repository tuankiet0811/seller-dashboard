using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public partial class RegisterExcelTests : PageTest
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
        await Page.GotoAsync($"{_baseUrl}/Account/Register");
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
    public async Task TCDK01_Register_Success()
    {
        var uniqueEmail = $"user_{System.Guid.NewGuid()}@gmail.com";
        await Page.FillAsync("input[name='Email']", uniqueEmail);
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123456!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "User123456!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page).ToHaveURLAsync(new Regex(".*/$|.*/Home/Index.*"));
    }

    [Test]
    public async Task TCDK02_Register_Another_Success()
    {
        var uniqueEmail = $"user_{System.Guid.NewGuid()}@gmail.com";
        await Page.FillAsync("input[name='Email']", uniqueEmail);
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "Password123456!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "Password123456!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page).ToHaveURLAsync(new Regex(".*/$|.*/Home/Index.*"));
    }

    [Test]
    public async Task TCDK03_Register_Another_Success_2()
    {
        var uniqueEmail = $"user_{System.Guid.NewGuid()}@gmail.com";
        await Page.FillAsync("input[name='Email']", uniqueEmail);
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "ComplexPass123456!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "ComplexPass123456!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page).ToHaveURLAsync(new Regex(".*/$|.*/Home/Index.*"));
    }

    [Test]
    public async Task TCDK04_Register_Fail_Email_Exists_Fail()
    {
        await Page.FillAsync("input[name='Email']", "admin1@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Địa chỉ email đã tồn tại", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDK05_Register_Fail_Invalid_Email_Fail()
    {
        await Page.FillAsync("input[name='Email']", "honguyendangkhoi1711gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='Email']"))
            .ToContainTextAsync("Email không tồn tại trên hệ thống", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDK06_Register_Fail_Password_Too_Short_Fail()
    {
        await Page.FillAsync("input[name='Email']", "newuser@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "123");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "123");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='Password']"))
            .ToContainTextAsync("Mật khẩu phải từ 6 ký tự trở lên", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDK07_Register_Fail_Empty_Email_Fail()
    {
        await Page.FillAsync("input[name='Email']", "");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='Email']"))
            .ToContainTextAsync("Không được để trống Email", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDK08_Register_Fail_Password_Space_Fail()
    {
        await Page.FillAsync("input[name='Email']", "spacepass@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "    ");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "    ");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Mật khẩu không hợp lệ", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDK09_Register_Fail_Password_Mismatch_Fail()
    {
        await Page.FillAsync("input[name='Email']", "mismatch@gmail.com");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='Password']", "User123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.FillAsync("input[name='ConfirmPassword']", "Different123!");
        await Page.WaitForTimeoutAsync(1500);
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("span[data-valmsg-for='ConfirmPassword']"))
            .ToContainTextAsync("Xác nhận mật khẩu không khớp", new() { Timeout = 3000 });
    }

    [Test]
    public async Task TCDK10_Register_Fail_Empty_All_Fail()
    {
        await Page.ClickAsync("button:has-text('Đăng ký')");
        await Page.WaitForTimeoutAsync(1500);
        
        await Expect(Page.Locator("div.text-red-500"))
            .ToContainTextAsync("Vui lòng nhập đầy đủ thông tin", new() { Timeout = 3000 });
    }
}
