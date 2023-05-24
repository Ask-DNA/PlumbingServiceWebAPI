using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using PlumbingServiceWebAPI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

string connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Api/User/Login";
        options.AccessDeniedPath = "/AccessDenied";
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddTransient<SessionService>();
builder.Services.AddTransient<OrderProcessingService>();
builder.Services.AddTransient<EmployeeManagementService>();
builder.Services.AddTransient<NotificationService>();

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "Api/{controller}/{action}");

app.Run();
