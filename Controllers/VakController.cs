using Microsoft.AspNetCore.Mvc;
using ChineseAcademicPortal.Models;
using System.Collections.Generic;
using System.Linq;

namespace ChineseAcademicPortal.Controllers
{
    public class VakController : Controller
    {
        // MVP: статические данные. Позже заменим на БД + парсинг vak.minobrnauki.gov.ru
        private static readonly List<VakJournal> _journals = new()
        {
            // Гуманитарные
            new VakJournal { Id = 1, NameRu = "Вопросы образования", NameEn = "Educational Studies Moscow", Category = "Гуманитарные", Issn = "1726-9482", Url = "https://vo.hse.ru/", Topics = "образование, педагогика, психология", Note = "Scopus, Web of Science" },
            new VakJournal { Id = 2, NameRu = "Социологические исследования", NameEn = "Sociological Studies", Category = "Гуманитарные", Issn = "0132-1625", Url = "https://www.isras.ru/index.php?page_id=118", Topics = "социология, общество, методы", Note = "Scopus" },
            new VakJournal { Id = 3, NameRu = "Философские науки", NameEn = "Philosophical Sciences", Category = "Гуманитарные", Issn = "0235-1188", Url = "https://iph.ras.ru/page49853561.html", Topics = "философия, этика, логика" },

            // Технические
            new VakJournal { Id = 4, NameRu = "Инженерный вестник Дона", NameEn = "Engineering Bulletin of Don", Category = "Технические", Issn = "2073-9985", Url = "http://ivdon.ru/", Topics = "IT, автоматизация, инженерия", Note = "Открытый доступ" },
            new VakJournal { Id = 5, NameRu = "Научный вестник МГТУ ГА", NameEn = "Scientific Bulletin of MSTU CA", Category = "Технические", Issn = "2073-2163", Url = "https://nv.mstuca.ru/", Topics = "авиация, транспорт, системы управления" },
            new VakJournal { Id = 6, NameRu = "Известия вузов. Серия «Электроника»", NameEn = "Izvestiya VUZ. Electronics", Category = "Технические", Issn = "1561-5405", Url = "https://journals.rudn.ru/engineering-technologies", Topics = "электроника, радиотехника, нанотехнологии" },

            // Естественные
            new VakJournal { Id = 7, NameRu = "Журнал экспериментальной и теоретической физики", NameEn = "Journal of Experimental and Theoretical Physics", Category = "Естественные", Issn = "0044-4510", Url = "https://jetp.ras.ru/", Topics = "физика, квантовая механика, оптика", Note = "Scopus, Springer" },
            new VakJournal { Id = 8, NameRu = "Экология", NameEn = "Russian Journal of Ecology", Category = "Естественные", Issn = "0367-0597", Url = "https://www.ecology-journal.ru/", Topics = "экология, биология, охрана природы", Note = "Scopus" },

            // Медицинские
            new VakJournal { Id = 9, NameRu = "Курский научно-практический вестник «Человек и его здоровье»", NameEn = "Kursk Scientific and Practical Bulletin", Category = "Медицинские", Issn = "1728-2322", Url = "https://www.knph.ru/", Topics = "медицина, здоровье, клинические исследования" },
            new VakJournal { Id = 10, NameRu = "Российский медицинский журнал", NameEn = "Russian Medical Journal", Category = "Медицинские", Issn = "0869-2084", Url = "https://www.rmj.ru/", Topics = "терапия, хирургия, фармакология", Note = "ВАК, РИНЦ" }
        };

        public IActionResult Index(string category, string topic)
        {
            var query = _journals.Where(j => j.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(j => j.Category == category);

            if (!string.IsNullOrEmpty(topic))
                query = query.Where(j => j.Topics.Contains(topic, System.StringComparison.OrdinalIgnoreCase));

            var journals = query.OrderBy(j => j.Category).ThenBy(j => j.NameRu).ToList();

            // Передаём категории и тематики для фильтров
            ViewBag.Categories = _journals.Select(j => j.Category).Distinct().OrderBy(c => c).ToList();
            ViewBag.Topics = _journals.SelectMany(j => j.Topics.Split(',').Select(t => t.Trim())).Distinct().OrderBy(t => t).ToList();
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedTopic = topic;

            return View(journals);
        }
    }
}