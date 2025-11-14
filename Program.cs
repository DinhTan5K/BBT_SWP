using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using start.Data;
using start.Models.Configurations;
using start.Services;
using start.Services.Interfaces;
using start.Services.Implementations.ECommerce;
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
builder.Services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IPayrollService, PayrollService >();
builder.Services.AddScoped<IMarketingKPIService, MarketingKPIService>();
builder.Services.AddScoped<IDayOffService, DayOffService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IAdminSecurityService, AdminSecurityService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IRegionService, RegionService>();

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
    // Đảm bảo session cookie name là unique để tránh conflict
    options.Cookie.Name = ".AspNetCore.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<EmailService>();
builder.Services.AddControllersWithViews();

// THÊM VÀO: Cấu hình chính sách cookie toàn cục để xử lý SameSite
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    // Chỉ bắt buộc Secure trong production (HTTPS), cho phép HTTP trong development
    options.Secure = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.None 
        : CookieSecurePolicy.Always; 
});


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CustomerScheme";
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie("AdminScheme", options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.Name = "AdminAuth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
})
.AddCookie("EmployeeScheme", options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.Name = "EmployeeAuth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
})
.AddCookie("CustomerScheme", options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.Name = "CustomerAuth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    googleOptions.CallbackPath = "/signin-google";
    googleOptions.Events.OnRemoteFailure = ctx =>
    {
        // Log lỗi kết nối Google OAuth
        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ctx.Failure, "Lỗi khi kết nối với Google OAuth: {Error}", ctx.Failure?.Message);
        
        // Redirect về trang login với thông báo lỗi
        ctx.Response.Redirect("/Account/Login?error=google_connection_failed");
        ctx.HandleResponse();
        return Task.CompletedTask;
    };
    googleOptions.Events.OnTicketReceived = async ctx =>
    {
        var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var email = ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            var customer = db.Customers.FirstOrDefault(c => c.Email == email);
            if (customer != null)
            {
                // Tạo Claims cho Customer
                // Role: CU (Customer)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                    new Claim(ClaimTypes.Name, customer.Name ?? ""),
                    new Claim(ClaimTypes.Email, customer.Email ?? ""),
                    new Claim("Role", "CU")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CustomerScheme");
                var principal = new ClaimsPrincipal(claimsIdentity);

                await ctx.HttpContext.SignInAsync("CustomerScheme", principal);
            }
        }
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}

// SỬA LẠI: Chỉ bật HTTPS trong production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// THÊM VÀO: Sử dụng Cookie Policy (phải đặt trước UseAuthentication và UseSession)
app.UseCookiePolicy(); 

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapStaticAssets();
app.UseStaticFiles();


app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "employee",
    pattern: "Employee/{action=Profile}/{id?}",
    defaults: new { controller = "Employee" });

app.MapControllerRoute(
    name: "productDetail",
    pattern: "Product/Detail/{id}",
    defaults: new { controller = "Product", action = "Detail" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();