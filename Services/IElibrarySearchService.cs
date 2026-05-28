using System.Collections.Generic;
using System.Threading.Tasks;
using ChineseAcademicPortal.Models;

namespace ChineseAcademicPortal.Services
{
    public interface IElibrarySearchService
    {
        Task<List<ElibraryArticle>> SearchAsync(string query, int page = 1);
    }
}