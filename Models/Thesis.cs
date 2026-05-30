using System.ComponentModel.DataAnnotations;
namespace ChineseAcademicPortal.Models
{
    public class Thesis
    {
        [Key]  // ✅ Обязательно: первичный ключ
        public int Id { get; set; }
        public string Title { get; set; }      // Название диссертации
        public string Author { get; set; }     // Автор
        public string Speciality { get; set; } // Шифр специальности (если есть)
        public string Year { get; set; }       // Год защиты
        public string Url { get; set; }        // Ссылка на карточку диссертации
    }
}