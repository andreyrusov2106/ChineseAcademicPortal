using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using ChineseAcademicPortal.Services;
using System.Text;
using Microsoft.Extensions.Localization; // <-- добавь это
using System;  // для StringComparison
using Npgsql.EntityFrameworkCore.PostgreSQL;  // для UseNpgsql

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// MVC + локализация (минимальная настройка)
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Настройка локализации
var supportedCultures = new[]
{
    new CultureInfo("ru-RU"),
    new CultureInfo("zh-CN")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ru-RU");
    options.SupportedCultures = supportedCultures;           // <-- IList<CultureInfo>
    options.SupportedUICultures = supportedCultures;         // <-- IList<CultureInfo>
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));*/
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // 🔍 Логирование для отладки (видно в Render Logs)
    var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    logger?.LogInformation("Connection string: '{Conn}'", connectionString ?? "(null)");

    // Определяем провайдер: если есть "Host=" или "Server=" или "Data Source=/" — это PostgreSQL
    // Если есть "Data Source=*.db" — это SQLite
    var isPostgres = !string.IsNullOrWhiteSpace(connectionString) &&
        (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
         !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase));

    if (isPostgres)
    {
        logger?.LogInformation("Using PostgreSQL provider");
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Fallback на SQLite для локальной разработки
        var sqlitePath = builder.Environment.IsDevelopment()
            ? "Data Source=academic.db"
            : "Data Source=/app/data/academic.db";
        logger?.LogInformation("Using SQLite provider with: {Path}", sqlitePath);
        options.UseSqlite(sqlitePath);
    }
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Home/Error";
    });

// Регистрация сервисов поиска
builder.Services.AddLogging(); // Включает поддержку ILogger<T>
builder.Services.AddSingleton<IArticleSearchService, OpenAlexSearchService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Временная регистрация: используем CyberLeninka как основной
//builder.Services.AddSingleton<IArticleSearchService>(sp =>
//    sp.GetRequiredService<CyberLeninkaSearchService>());

var app = builder.Build();
// Логирование конфигурации при старте
var config = app.Services.GetRequiredService<IConfiguration>();
var connStr = config.GetConnectionString("DefaultConnection");
Console.WriteLine($"🔐 ConnectionString at startup: {(string.IsNullOrWhiteSpace(connStr) ? "(EMPTY)" : connStr.Substring(0, Math.Min(50, connStr.Length)) + "...")}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLocalization(); // после UseRouting!

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();