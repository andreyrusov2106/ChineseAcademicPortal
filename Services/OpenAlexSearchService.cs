using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ChineseAcademicPortal.Models;
using Microsoft.Extensions.Logging;

namespace ChineseAcademicPortal.Services
{
    public class OpenAlexSearchService : IArticleSearchService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        private readonly ILogger<OpenAlexSearchService> _logger;

        public OpenAlexSearchService(ILogger<OpenAlexSearchService> logger)
        {
            _logger = logger;
        }

        public async Task<List<Article>> SearchAsync(string query, int page = 1, int? yearFrom = null, int? yearTo = null, string topic = null, string author = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Article>();

            try
            {
                var filters = new List<string> { "is_oa:true" };

                if (yearFrom.HasValue || yearTo.HasValue)
                    filters.Add($"publication_year:{yearFrom ?? 1900}-{yearTo ?? DateTime.Now.Year}");

                if (!string.IsNullOrWhiteSpace(topic))
                    filters.Add($"concepts.display_name.search:{Uri.EscapeDataString(topic)}");

                // 🔍 ИСПРАВЛЕНО: raw_author_name.search вместо authorships.author.display_name.search
                if (!string.IsNullOrWhiteSpace(author))
                    filters.Add($"raw_author_name.search:{Uri.EscapeDataString(author)}");

                var filterParam = string.Join(",", filters);
                var url = $"https://api.openalex.org/works?search={Uri.EscapeDataString(query)}" +
                          $"&filter={filterParam}" +
                          $"&sort=cited_by_count:desc" +
                          $"&per_page=20";

                if (page > 1) url += $"&page={page}";

                _logger.LogInformation("OpenAlex request: {Url}", url);
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAlex error {Code}: {Body}", response.StatusCode, error);
                    return new List<Article>();
                }

                var json = await response.Content.ReadFromJsonAsync<OpenAlexResponse>();
                if (json?.Results == null) return new List<Article>();

                var articles = new List<Article>();
                foreach (var item in json.Results)
                {
                    if (string.IsNullOrEmpty(item.Title)) continue;

                    var pdfUrl = item.PrimaryLocation?.PdfUrl;
                    var landingUrl = item.LandingPageUrl;
                    var doi = item.Dois?.FirstOrDefault();
                    var finalUrl = !string.IsNullOrEmpty(pdfUrl) ? pdfUrl
                                 : !string.IsNullOrEmpty(landingUrl) ? landingUrl
                                 : !string.IsNullOrEmpty(doi) ? $"https://doi.org/{doi}" : "#";

                    articles.Add(new Article
                    {
                        Title = item.Title,
                        Authors = string.Join(", ", item.Authorships?.Take(3).Select(a => a.Author?.DisplayName).Where(n => !string.IsNullOrEmpty(n)) ?? new List<string>()),
                        Source = item.PrimaryLocation?.Source?.DisplayName ?? "OpenAlex",
                        Url = finalUrl,
                        ExtraData = new Dictionary<string, string>
                        {
                            ["elibrary_deep_link"] = $"https://elibrary.ru/query_results.asp?text={Uri.EscapeDataString(item.Title)}",
                            ["openalex_id"] = item.Id?.Replace("https://openalex.org/", ""),
                            ["year"] = item.PublicationYear
                        }
                    });
                }
                return articles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAlex search failed");
                return new List<Article>();
            }
        }
    }

    // === Минимальные модели для десериализации ===
    public class OpenAlexResponse
    {
        public List<OpenAlexWork> Results { get; set; }
        public MetaData Meta { get; set; }
    }
    public class MetaData
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int TotalResults { get; set; }
    }
    public class OpenAlexWork
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string LandingPageUrl { get; set; }
        public List<OpenAlexAuthorship> Authorships { get; set; }
        public OpenAlexLocation PrimaryLocation { get; set; }
        public List<string> Dois { get; set; }
        public int? CitedByCount { get; set; }
        public string? PublicationYear { get; set; }
    }
    public class OpenAlexAuthorship
    {
        public OpenAlexAuthor Author { get; set; }
    }
    public class OpenAlexAuthor
    {
        public string DisplayName { get; set; }
    }
    public class OpenAlexLocation
    {
        public string PdfUrl { get; set; }
        public OpenAlexSource Source { get; set; }
    }
    public class OpenAlexSource
    {
        public string DisplayName { get; set; }
        public List<string> CountryCodes { get; set; }
    }
}