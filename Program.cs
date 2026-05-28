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

    // 🔍 Регистронезависимая проверка на PostgreSQL
    var isPostgres = connectionString?.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0
                  || connectionString?.IndexOf("Username=", StringComparison.OrdinalIgnoreCase) >= 0;

    if (isPostgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
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