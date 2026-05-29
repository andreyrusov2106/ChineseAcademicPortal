using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
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

            ViewBag.YearFrom = yearFrom;
            ViewBag.YearTo = yearTo;
            ViewBag.Topic = topic;
            ViewBag.Author = author; // <-- сохраняем для формы

            var articles = await _searchService.SearchAsync(q, page, yearFrom, yearTo, topic, author);

            return View(new SearchResultViewModel
            {
                Query = q,
                Articles = articles,
                Page = page
            });
        }

        public IActionResult ExportCsv(string q, int? yearFrom, int? yearTo, string author)
        {
            var articles = await _searchService.SearchAsync(q, 1, yearFrom, yearTo, author);

            var csv = new StringBuilder();
            csv.AppendLine("Title,Authors,Source,Url");
            foreach (var a in articles)
            {
                csv.AppendLine($"\"{a.Title}\",\"{a.Authors}\",\"{a.Source}\",\"{a.Url}\"");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"search_results_{DateTime.Now:yyyyMMdd}.csv");
        }
    }

    public class SearchResultViewModel
    {
        public string Query { get; set; }
        public List<Article> Articles { get; set; }
        public int Page { get; set; }
        public int TotalResults { get; set; }
        public int PageSize => 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
    }
}