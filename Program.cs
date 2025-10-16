using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using start.Data;
using start.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ... (Các service của bạn không thay đổi)
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderReadService, OrderReadService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.Configure<start.Models.Configurations.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<AiService>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.TimestampFormat = "HH:mm:ss ";
    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<EmailService>();
builder.Services.AddControllersWithViews();

// THÊM VÀO: Cấu hình chính sách cookie toàn cục để xử lý SameSite
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    // Bắt buộc tất cả cookie phải là Secure, chỉ gửi qua HTTPS
    options.Secure = CookieSecurePolicy.Always; 
});


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    // Cấu hình cookie này có thể để mặc định hoặc Lax, 
    // vì CookiePolicy ở trên đã thiết lập mức tối thiểu.
    // Để an toàn, bạn có thể chỉnh lại cho nhất quán.
    options.Cookie.SameSite = SameSiteMode.Lax; 
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    googleOptions.CallbackPath = "/signin-google";
    googleOptions.Events.OnTicketReceived = ctx =>
    {
        // ... (Phần code xử lý logic sau khi đăng nhập của bạn giữ nguyên)
        var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var email = ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            var customer = db.Customers.FirstOrDefault(c => c.Email == email);
            if (customer != null)
            {
                ctx.HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                ctx.HttpContext.Session.SetString("CustomerName", customer.Name ?? "");
                var identity = (ClaimsIdentity)ctx.Principal?.Identity!;
                var existingName = identity.FindFirst(ClaimTypes.Name);
                if (existingName != null)
                {
                    identity.RemoveClaim(existingName);
                }
                identity.AddClaim(new Claim(ClaimTypes.Name, customer.Name ?? ""));
            }
        }
        return Task.CompletedTask;
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// SỬA LẠI: Luôn bật HTTPS cho cả dev và production
app.UseHttpsRedirection();

app.UseRouting();

// THÊM VÀO: Sử dụng Cookie Policy
app.UseCookiePolicy(); 

app.UseSession();

// app.UseAntiforgery(); // Dòng này thường không cần thiết ở đây nếu bạn dùng [ValidateAntiForgeryToken]
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads")),
    RequestPath = "/uploads"
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();