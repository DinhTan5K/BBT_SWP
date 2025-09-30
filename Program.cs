using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using start.Data;
using start.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<start.Models.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<EmailService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<EmailService>();
builder.Services.AddControllersWithViews();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

})

.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    googleOptions.CallbackPath = "/signin-google";
     googleOptions.Events.OnTicketReceived = ctx =>
{
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
    app.UseHttpsRedirection();
    app.UseHsts();
}


app.UseRouting();
app.UseSession();
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
