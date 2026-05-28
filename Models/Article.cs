public class Article
{
    public string Title { get; set; }
    public string Authors { get; set; }
    public string Source { get; set; }
    public string Url { get; set; }

    // Добавь это свойство:
    public Dictionary<string, string> ExtraData { get; set; } = new();
}