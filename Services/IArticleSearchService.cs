using System.Collections.Generic;
using System.Threading.Tasks;
using ChineseAcademicPortal.Models;

namespace ChineseAcademicPortal.Services
{
    public interface IArticleSearchService
    {
        Task<List<Article>> SearchAsync(string query, int page = 1, int? yearFrom = null, int? yearTo = null, string topic = null, string author = null);
    }
}