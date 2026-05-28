using System.Collections.Generic;
using System.Threading.Tasks;
using ChineseAcademicPortal.Models;

namespace ChineseAcademicPortal.Services
{
    public interface IThesisSearchService
    {
        Task<List<Thesis>> SearchAsync(string author, string title, string speciality, int? yearFrom, int? yearTo, int page = 1);
    }
}