namespace ChineseAcademicPortal.Models
{
    public class VakJournal
    {
        public int Id { get; set; }
        public string NameRu { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string Category { get; set; } = ""; // Гуманитарные, Технические, Естественные, Медицинские
        public string Issn { get; set; } = "";
        public string Url { get; set; } = "";
        public string Topics { get; set; } = ""; // Тематики через запятую
        public bool IsActive { get; set; } = true;
        public string Note { get; set; } = ""; // Примечание (например, "входит в Scopus")
        public DateTime? UpdatedAt { get; set; }
        public string SearchQuery { get; set; } = ""; // Для авто-поиска журнала
    }
}