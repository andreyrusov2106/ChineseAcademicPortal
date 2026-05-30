// Controllers/Admin/VakImportController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChineseAcademicPortal.Services;

namespace ChineseAcademicPortal.Controllers.Admin;

[Authorize] // Только для админов
public class VakImportController : Controller
{
    private readonly VakJsonImportService _importService;

    public VakImportController(VakJsonImportService importService)
    {
        _importService = importService;
    }

    [HttpGet]
    public async Task<IActionResult> RunImport()
    {
        try
        {
            var count = await _importService.ImportFromJsonAsync();
            TempData["Success"] = $"✅ Импортировано {count} журналов ВАК";
            return RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"❌ Ошибка импорта: {ex.Message}";
            return RedirectToAction("Index", "Admin");
        }
    }
}