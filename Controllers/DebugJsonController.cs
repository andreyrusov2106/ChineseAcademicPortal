// Controllers/DebugJsonController.cs
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace ChineseAcademicPortal.Controllers;

public class DebugJsonController : Controller
{
    private readonly HttpClient _http;

    public DebugJsonController(HttpClient http) => _http = http;

    [HttpGet]
    public async Task<IActionResult> Show()
    {
        try
        {
            var json = await _http.GetStringAsync("https://journalrank.rcsi.science/ru/record-sources/download/?dataType=Json");

            // Показываем первые 2000 символов
            var preview = json.Length > 2000 ? json.Substring(0, 2000) + "..." : json;

            return Content($"Длина JSON: {json.Length} символов\n\nПервые записи:\n{preview}", "text/plain");
        }
        catch (Exception ex)
        {
            return Content($"Ошибка: {ex.Message}");
        }
    }
}