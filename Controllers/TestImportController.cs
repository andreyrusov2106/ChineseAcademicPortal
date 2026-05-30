// Controllers/TestImportController.cs
using Microsoft.AspNetCore.Mvc;
using ChineseAcademicPortal.Services;

namespace ChineseAcademicPortal.Controllers;

public class TestImportController : Controller
{
    private readonly VakJsonImportService _service;

    public TestImportController(VakJsonImportService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Run()
    {
        try
        {
            var count = await _service.ImportFromJsonAsync();
            return Content($"✅ Успешно! Импортировано {count} журналов.\n\nПроверь таблицу VakJournals в БД.");
        }
        catch (Exception ex)
        {
            return Content($"❌ Ошибка: {ex.Message}\n\n{ex.StackTrace}");
        }
    }
}