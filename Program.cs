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

    // Логирование для отладки
    var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    var connDisplay = string.IsNullOrWhiteSpace(connectionString)
        ? "(null)"
        : connectionString.Length > 60
            ? connectionString.Substring(0, 60) + "..."
            : connectionString;
    logger?.LogInformation("ConnectionString: {Conn}", connDisplay);

    // 🔍 Определение провайдера:
    // - PostgreSQL: содержит "postgresql://", "postgres://", "Host=", "Server=" (но не "Data Source=*.db")
    // - SQLite: содержит "Data Source=*.db" или пустая строка
    var isPostgres = !string.IsNullOrWhiteSpace(connectionString) &&
        (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
         connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
         (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
          connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)) &&
         !connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase));

    if (isPostgres)
    {
        logger?.LogInformation("✅ Using PostgreSQL provider");
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Fallback на SQLite для локальной разработки
        var sqlitePath = builder.Environment.IsDevelopment()
            ? "Data Source=academic.db"
            : "Data Source=/app/data/academic.db";
        logger?.LogInformation("✅ Using SQLite provider: {Path}", sqlitePath);
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
// Применяем миграции при старте (только для PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var connStr = context.Database.GetDbConnection().ConnectionString;

        // Проверяем, что это PostgreSQL
        if (connStr?.StartsWith("postgresql://") == true || connStr?.StartsWith("postgres://") == true || connStr?.Contains("Host=") == true)
        {
            logger?.LogInformation("🔄 Applying PostgreSQL migrations...");
            var pending = context.Database.GetPendingMigrations();
            if (pending.Any())
            {
                logger?.LogInformation("Pending migrations: {Migrations}", string.Join(", ", pending));
                context.Database.Migrate();
                logger?.LogInformation("✅ Migrations applied successfully");
            }
            else
            {
                logger?.LogInformation("✅ Database is up to date");
            }
        }
    }
    catch (Exception ex)
    {
        logger?.LogError(ex, "❌ Migration error");
        // Не падаем, чтобы не ломать деплой, но логируем
    }
}

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