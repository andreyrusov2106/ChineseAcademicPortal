using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using HtmlAgilityPack;
using ChineseAcademicPortal.Models;

namespace ChineseAcademicPortal.Services
{
    public class RslThesisSearchService : IThesisSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(1);
        private DateTime _lastRequestTime = DateTime.MinValue;

        public RslThesisSearchService(IMemoryCache memoryCache)
        {
            _httpClient = new HttpClient();
            _cache = memoryCache;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<List<Thesis>> SearchAsync(string author, string title, string speciality, int? yearFrom, int? yearTo, int page = 1)
        {
            // Формируем поисковую строку
            var searchTerms = new List<string>();
            if (!string.IsNullOrWhiteSpace(author)) searchTerms.Add($"author:{author}");
            if (!string.IsNullOrWhiteSpace(title)) searchTerms.Add($"title:{title}");
            if (!string.IsNullOrWhiteSpace(speciality)) searchTerms.Add($"spec:{speciality}");

            if (searchTerms.Count == 0)
                return new List<Thesis>();

            string query = string.Join(" ", searchTerms); // пробел, а не AND
            string cacheKey = $"rsl_get_{query}_{yearFrom}_{yearTo}_{page}";
            if (_cache.TryGetValue(cacheKey, out List<Thesis> cached))
                return cached;

            // Строим URL для GET-запроса (без #)
            var url = $"https://search.rsl.ru/ru/search#q={Uri.EscapeDataString(query)}";
            if (yearFrom.HasValue) url += $"&yf={yearFrom.Value}";
            if (yearTo.HasValue) url += $"&yl={yearTo.Value}";
            if (page > 1) url += $"&page={page}";

            Console.WriteLine($"[DEBUG] URL: {url}");

            await Task.Delay(_delay);
            _lastRequestTime = DateTime.Now;

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<Thesis>();

            var html = await response.Content.ReadAsStringAsync();
            System.IO.File.WriteAllText("debug.html", html);
            if (html.Contains("Нет результатов") || html.Length < 1000)
                return new List<Thesis>();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var theses = new List<Thesis>();
            // Ищем блоки с классом "search-item" или "search-container"
            var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'search-item')]");
            if (items == null)
                items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'search-container')]");
            if (items == null)
                return theses;

            foreach (var item in items)
            {
                try
                {
                    // Автор
                    var authorNode = item.SelectSingleNode(".//b[contains(@class, 'js-item-authorinfo')]");
                    var authorText = authorNode?.InnerText.Trim() ?? "";

                    // Описание (название + источник)
                    var infoNode = item.SelectSingleNode(".//span[contains(@class, 'js-item-maininfo')]");
                    var description = infoNode?.InnerText.Trim() ?? "";

                    var titleText = description;
                    var source = "";
                    var dashIndex = description.IndexOf('-');
                    if (dashIndex > 0)
                    {
                        titleText = description.Substring(0, dashIndex).Trim();
                        source = description.Substring(dashIndex + 1).Trim();
                    }

                    // Ссылка
                    var linkNode = item.SelectSingleNode(".//a[contains(@class, 'rsl-modal')]");
                    var urlRef = linkNode?.GetAttributeValue("href", "") ?? "";
                    if (!string.IsNullOrEmpty(urlRef) && !urlRef.StartsWith("http"))
                        urlRef = "https://search.rsl.ru" + urlRef;

                    // Год
                    int year = 0;
                    if (!string.IsNullOrEmpty(source))
                    {
                        var yearMatch = System.Text.RegularExpressions.Regex.Match(source, @"\b(19|20)\d{2}\b");
                        if (yearMatch.Success) int.TryParse(yearMatch.Value, out year);
                    }

                    theses.Add(new Thesis
                    {
                        Title = titleText,
                        Author = authorText,
                        Speciality = speciality,
                        Year = year > 0 ? year.ToString() : "",
                        Url = urlRef
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Parse error: {ex.Message}");
                }
            }

            _cache.Set(cacheKey, theses, TimeSpan.FromHours(1));
            return theses;
        }
    }
}