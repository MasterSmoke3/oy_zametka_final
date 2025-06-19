using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZametkiApp.Data;
using Microsoft.Extensions.Configuration;
using ZametkiApp.Services;
using ZametkiApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Получаем строку подключения из конфигурации
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Проверяем, что передана строка для PostgreSQL (через env var, например)
var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres", false);

builder.Services.AddHostedService<DeadlineNotifierService>();

if (usePostgres && !string.IsNullOrWhiteSpace(connectionString))
{
    // Используем PostgreSQL, добавляем sslmode=require в строку, если нет
    if (!connectionString.Contains("sslmode"))
    {
        connectionString += connectionString.Contains("?") ? "&sslmode=require" : "?sslmode=require";
    }
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // По умолчанию SQLite локальная
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=notes.db"));
}

builder.Services.AddSignalR();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Account/ResetPassword");
});

builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Path: {context.Request.Path}");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
