using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using ChineseAcademicPortal.Services;
using ChineseAcademicPortal;
using System.Text;
using Microsoft.Extensions.Localization;
using Npgsql.EntityFrameworkCore.PostgreSQL;  // ✅ Обязательно для PostgreSQL
using Microsoft.Extensions.Logging;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// MVC + локализация
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Локализация
var supportedCultures = new[]
{
    new CultureInfo("ru-RU"),
    new CultureInfo("zh-CN")
};
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ru-RU");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// 🔍 БЛОК ПОДКЛЮЧЕНИЯ К БД — ИСПРАВЛЕННЫЙ
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();

    // Показываем первые 80 символов строки (без пароля)
    var connDisplay = string.IsNullOrWhiteSpace(connStr) ? "(EMPTY)"
        : connStr.Length > 80 ? connStr.Substring(0, 80) + "..." : connStr;
    logger?.LogInformation("🔐 ConnectionString: {Conn}", connDisplay);

    // 🔥 ОПРЕДЕЛЕНИЕ ПРОВАЙДЕРА — ПОЛНОЕ ПОКРЫТИЕ
    // PostgreSQL: URI-формат (Render) ИЛИ ключ-значение (локально)
    var isPostgres = !string.IsNullOrWhiteSpace(connStr) &&
        (connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
         connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
         connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
         connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase));

    if (isPostgres)
    {
        logger?.LogInformation("✅ Using PostgreSQL provider");
        options.UseNpgsql(connStr);
    }
    else
    {
        // SQLite для локальной разработки
        var sqlitePath = builder.Environment.IsDevelopment()
            ? "Data Source=academic.db"
            : "Data Source=/app/data/academic.db";
        logger?.LogInformation("✅ Using SQLite provider: {Path}", sqlitePath);
        options.UseSqlite(sqlitePath);
    }
});

// Аутентификация
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Home/Error";
    });

// Сервисы поиска
builder.Services.AddSingleton<IArticleSearchService, OpenAlexSearchService>();
builder.Services.AddSingleton<IThesisSearchService, RslThesisSearchService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
builder.Services.AddHttpClient<VakJsonImportService>();
builder.Services.AddHostedService<VakAutoUpdateService>();

var app = builder.Build();

// 🔥 ПРИМЕНЕНИЕ МИГРАЦИЙ ДЛЯ POSTGRESQL
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var conn = context.Database.GetDbConnection().ConnectionString;

        // Проверяем, что это PostgreSQL
        if (!string.IsNullOrWhiteSpace(conn) &&
            (conn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
             conn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
             conn.Contains("Host=", StringComparison.OrdinalIgnoreCase)))
        {
            logger?.LogInformation("🔄 Applying PostgreSQL migrations...");
            var pending = context.Database.GetPendingMigrations().ToList();
            if (pending.Any())
            {
                logger?.LogInformation("📋 Pending: {Migrations}", string.Join(", ", pending));
                context.Database.Migrate();
                logger?.LogInformation("✅ Migrations applied");
            }
            else
            {
                logger?.LogInformation("✅ Database is up to date");
            }
        }
    }
    catch (Exception ex)
    {
        logger?.LogError(ex, "❌ Migration failed");
    }
}


//builder.Services.AddHostedService<VakImportBackgroundService>();

// Middleware
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
app.UseRequestLocalization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

if (args.Contains("--import-vak-only"))
{
    using var scope = app.Services.CreateScope();
    var importService = scope.ServiceProvider.GetRequiredService<VakJsonImportService>();
    var count = await importService.ImportFromJsonAsync();
    Console.WriteLine($"✅ GitHub Actions: Updated {count} VAK journals");
    return; // Завершаем приложение после импорта
}

await app.RunAsync();