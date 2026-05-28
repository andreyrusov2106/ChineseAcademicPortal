using System.ComponentModel.DataAnnotations;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введите название на русском")]
    [Display(Name = "Название (русский)")]
    public string? NameRu { get; set; }

    [Required(ErrorMessage = "请输入中文名称")]
    [Display(Name = "Название (китайский)")]
    public string? NameZh { get; set; }

    // Навигационное свойство
    public List<Link> Links { get; set; } = new();
}