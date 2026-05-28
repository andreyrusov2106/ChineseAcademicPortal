using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using ChineseAcademicPortal.Models;
using System.Net;

namespace ChineseAcademicPortal.Services
{
    public class ElibrarySearchService : IElibrarySearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _delay = TimeSpan.FromSeconds(2);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private bool _isLoggedIn = false;

        public ElibrarySearchService(IMemoryCache memoryCache, IConfiguration configuration)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };
            _httpClient = new HttpClient(handler);
            _cache = memoryCache;
            _configuration = configuration;

            // Заголовки как у обычного браузера
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        }

        private async Task<bool> LoginAsync()
        {
            var login = _configuration["Elibrary:Login"];
            var password = _configuration["Elibrary:Password"];
            Console.WriteLine($"[DEBUG] Attempt login with: {login}"); // не показывайте пароль

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("[DEBUG] Login or password empty");
                return false;
            }

            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("login", login),
        new KeyValuePair<string, string>("pass", password),
        new KeyValuePair<string, string>("submit", "Вход")
    });

            var loginUrl = "https://elibrary.ru/login.asp";
            var response = await _httpClient.PostAsync(loginUrl, content);
            Console.WriteLine($"[DEBUG] Login response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseHtml = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Login response length: {responseHtml.Length}");
                // Выведем первые 500 символов, чтобы увидеть, есть ли ошибка входа
                Console.WriteLine($"[DEBUG] Login response preview: {responseHtml.Substring(0, Math.Min(500, responseHtml.Length))}");

                if (responseHtml.Contains("/end_session.asp") || responseHtml.Contains("Личный кабинет"))
                {
                    _isLoggedIn = true;
                    return true;
                }
                else
                {
                    Console.WriteLine("[DEBUG] Login failed: no session or cabinet indicators");
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] Login HTTP error: {response.StatusCode}");
            }
            return false;
        }

        public async Task<List<ElibraryArticle>> SearchAsync(string query, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ElibraryArticle>();

            string cacheKey = $"elibrary_{query}_{page}";
            if (_cache.TryGetValue(cacheKey, out List<ElibraryArticle> cached))
                return cached;

            // Авторизуемся, если ещё нет
            if (!_isLoggedIn)
            {
                var loginSuccess = await LoginAsync();
                if (!loginSuccess)
                {
                    Console.WriteLine("[DEBUG] Login failed, cannot search.");
                    return new List<ElibraryArticle>();
                }
            }

            // Задержка между запросами
            var timeSinceLast = DateTime.Now - _lastRequestTime;
            if (timeSinceLast < _delay)
                await Task.Delay(_delay - timeSinceLast);
            _lastRequestTime = DateTime.Now;

            string url = $"https://elibrary.ru/query_results.asp?p={page}&q={Uri.EscapeDataString(query)}";
            Console.WriteLine($"[DEBUG] Search URL: {url}");

            var response = await _httpClient.GetAsync(url);
            Console.WriteLine($"[DEBUG] Search response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("[DEBUG] Search HTTP error.");
                return new List<ElibraryArticle>();
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            string html;
            try
            {
                html = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                var encoding = Encoding.GetEncoding(1251);
                html = encoding.GetString(bytes);
            }

            Console.WriteLine($"[DEBUG] Search response length: {html.Length}");
            if (html.Length > 0)
                Console.WriteLine($"[DEBUG] Search response preview: {html.Substring(0, Math.Min(500, html.Length))}");

            // Если страница содержит капчу или требует логин, сбрасываем флаг
            if (html.Contains("captcha") || html.Contains("Введите контрольное число") || html.Contains("EndSession"))
            {
                Console.WriteLine("[DEBUG] Captcha or session expired detected");
                _isLoggedIn = false;
                return new List<ElibraryArticle>();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//tr[starts-with(@id, 'a')]");
            Console.WriteLine($"[DEBUG] Rows found with id='a...': {rows?.Count ?? 0}");

            if (rows == null || rows.Count == 0)
            {
                // Выведем все строки для анализа
                var allRows = doc.DocumentNode.SelectNodes("//tr");
                Console.WriteLine($"[DEBUG] Total rows in document: {allRows?.Count ?? 0}");
                if (allRows != null && allRows.Count > 0)
                {
                    Console.WriteLine($"[DEBUG] First row sample: {allRows[0].OuterHtml.Substring(0, Math.Min(200, allRows[0].OuterHtml.Length))}");
                }
                return new List<ElibraryArticle>();
            }

            var articles = new List<ElibraryArticle>();
            foreach (var row in rows)
            {
                try
                {
                    var titleNode = row.SelectSingleNode(".//a[contains(@href, '/item.asp?id=')]");
                    if (titleNode == null) continue;
                    string title = titleNode.InnerText.Trim();
                    string relativeUrl = titleNode.GetAttributeValue("href", "");
                    string fullUrl = relativeUrl.StartsWith("http") ? relativeUrl : "https://elibrary.ru" + relativeUrl;

                    var authorsNode = row.SelectSingleNode(".//font[@color='#00008f']/i");
                    string authors = authorsNode != null ? authorsNode.InnerText.Trim() : "";

                    var sourceNodes = row.SelectNodes(".//font[@color='#00008f']");
                    string source = "";
                    if (sourceNodes != null && sourceNodes.Count >= 2)
                        source = sourceNodes[1].InnerText.Trim();

                    articles.Add(new ElibraryArticle
                    {
                        Title = title,
                        Authors = authors,
                        Source = source,
                        Url = fullUrl
                    });
                }
                catch { /* пропускаем проблемные строки */ }
            }

            Console.WriteLine($"[DEBUG] Parsed articles count: {articles.Count}");
            _cache.Set(cacheKey, articles, TimeSpan.FromHours(1));
            return articles;
        }
    }
}