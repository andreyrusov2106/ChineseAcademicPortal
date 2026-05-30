using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChineseAcademicPortal;          // ✅ AppDbContext в корневом namespace
using ChineseAcademicPortal.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ChineseAcademicPortal.Controllers;

public class VakController : Controller
{
    private readonly AppDbContext _db;

    public VakController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string topic, string indexType)
    {
        var query = _db.VakJournals.AsQueryable();

        // 🔍 Фильтр по типу индексации (через поле Note)
        if (!string.IsNullOrEmpty(indexType))
        {
            query = indexType switch
            {
                "ВАК" => query.Where(j => j.Note != null && j.Note.Contains("ВАК")),
                "Scopus" => query.Where(j => j.Note != null && j.Note.Contains("Scopus")),
                "WoS" => query.Where(j => j.Note != null && j.Note.Contains("WoS")),
                "Scopus+WoS" => query.Where(j => j.Note != null && j.Note.Contains("Scopus") && j.Note.Contains("WoS")),
                _ => query
            };
        }

        // 🔍 Поиск по названию
        if (!string.IsNullOrEmpty(topic))
        {
            query = query.Where(j => j.NameRu.Contains(topic) ||
                                    (j.Note != null && j.Note.Contains(topic)));
        }

        // Передаём текущие фильтры обратно в представление
        ViewBag.IndexType = indexType;
        ViewBag.SelectedTopic = topic;

        var journals = await query
            .OrderBy(j => j.NameRu)
            .ToListAsync();

        return View(journals);
    }
}