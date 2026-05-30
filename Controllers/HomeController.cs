using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ChineseAcademicPortal.Models;
using ChineseAcademicPortal;
namespace ChineseAcademicPortal.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories.Include(c => c.Links).ToListAsync();
        return View(categories);
    }

    // Действие для отображения ссылок одной категории
    public async Task<IActionResult> Category(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Links)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
            return NotFound();
        return View(category);
    }

    // Переключение языка через cookie
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = "/";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        return LocalRedirect(returnUrl);
    }

    // Обработка ошибок (опционально)
    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}