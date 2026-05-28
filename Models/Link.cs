using System.ComponentModel.DataAnnotations;

public class Link
{
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required(ErrorMessage = "Введите заголовок на русском")]
    [Display(Name = "Заголовок (русский)")]
    public string? TitleRu { get; set; }

    [Required(ErrorMessage = "请输入中文标题")]
    [Display(Name = "Заголовок (китайский)")]
    public string? TitleZh { get; set; }

    [Required]
    [Url]
    public string? Url { get; set; }

    [Display(Name = "Описание (русский)")]
    public string? DescriptionRu { get; set; }

    [Display(Name = "Описание (китайский)")]
    public string? DescriptionZh { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.Now;
}