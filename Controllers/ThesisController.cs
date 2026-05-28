using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ChineseAcademicPortal.Services;
using ChineseAcademicPortal.Models;

namespace ChineseAcademicPortal.Controllers
{
    public class ThesisController : Controller
    {
        private readonly IThesisSearchService _thesisService;

        public ThesisController(IThesisSearchService thesisService)
        {
            _thesisService = thesisService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ThesisSearchViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(ThesisSearchViewModel model, int page = 1)
        {
            Console.WriteLine($"Before search: Author={model.Author}, YearFrom={model.YearFrom}");
            model.Results = await _thesisService.SearchAsync(
                author: model.Author,
                title: model.Title,
                speciality: model.Speciality,
                yearFrom: model.YearFrom,
                yearTo: model.YearTo,
                page: page
            );
            Console.WriteLine($"After search: Results count = {model.Results?.Count ?? 0}");
            return View(model);
        }
    }

    // Обновленная модель представления
    public class ThesisSearchViewModel
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Speciality { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public List<Thesis> Results { get; set; } = new();
    }
}