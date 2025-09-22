    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.Google;
    using start.Data;
    using start.Services;
    using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

    var builder = WebApplication.CreateBuilder(args);


    // 1. Thêm cache để session hoạt động
    builder.Services.AddDistributedMemoryCache();

    // 2. Thêm session
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    } );
    builder.Services.AddHttpContextAccessor();
    
    builder.Services.AddTransient<EmailService>();

    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // Thêm Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.None; 
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

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

    // Configure the HTTP request pipeline.
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

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();


    app.Run();
   