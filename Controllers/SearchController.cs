using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;  // ✅ Для StringBuilder и Encoding
using ChineseAcademicPortal.Models;
using ChineseAcademicPortal.Services;

namespace ChineseAcademicPortal.Controllers
{
    public class SearchController : Controller
    {
        private readonly IArticleSearchService _searchService;

        public SearchController(IArticleSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string q, int page = 1, int? yearFrom = null, int? yearTo = null, string topic = null, string author = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(new SearchResultViewModel { Query = q, Articles = new List<Article>() });

            var articles = await _searchService.SearchAsync(q, page, yearFrom, yearTo, topic, author);

            // Сохраняем фильтры для представления
            ViewBag.YearFrom = yearFrom;
            ViewBag.YearTo = yearTo;
            ViewBag.Topic = topic;
            ViewBag.Author = author;

            var viewModel = new SearchResultViewModel
            {
                Query = q,
                Articles = articles,
                Page = page,
                TotalResults = articles.Count // Для MVP: показываем сколько нашли
            };
            return View(viewModel);
        }

        // ✅ ЭКСПОРТ В CSV — ИСПРАВЛЕННАЯ ВЕРСИЯ
        [HttpGet]
        public async Task<IActionResult> ExportCsv(string q, int? yearFrom = null, int? yearTo = null, string topic = null, string author = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index");

            // Получаем все результаты (для экспорта берём первую страницу, можно расширить)
            var articles = await _searchService.SearchAsync(q, 1, yearFrom, yearTo, topic, author);

            // Формируем CSV
            var csv = new StringBuilder();
            csv.AppendLine("Title,Authors,Source,Url,Year"); // Заголовки

            foreach (var a in articles)
            {
                // Экранируем кавычки и запятые в полях
                var title = a.Title?.Replace("\"", "\"\"") ?? "";
                var authors = a.Authors?.Replace("\"", "\"\"") ?? "";
                var source = a.Source?.Replace("\"", "\"\"") ?? "";
                var url = a.Url?.Replace("\"", "\"\"") ?? "";
                var year = a.ExtraData?.GetValueOrDefault("year") ?? "";

                csv.AppendLine($"\"{title}\",\"{authors}\",\"{source}\",\"{url}\",\"{year}\"");
            }

            // Возвращаем файл
            var fileName = $"search_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", fileName);
        }
    }

    // ✅ SearchResultViewModel с пагинацией
    public class SearchResultViewModel
    {
        public string Query { get; set; } = "";
        public List<Article> Articles { get; set; } = new();
        public int Page { get; set; } = 1;
        public int TotalResults { get; set; }
        public int PageSize => 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
    }
}