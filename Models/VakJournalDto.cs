using System.Text.Json.Serialization;

namespace ChineseAcademicPortal.Models;

public class VakJournalDto
{
    public List<string>? title { get; set; }
    public List<string>? issns { get; set; }
    public IndexStatus? rsci { get; set; }
    public IndexStatus? scopus { get; set; }
    public IndexStatus? wos_cc { get; set; }
}

public class IndexStatus
{
    public bool value { get; set; }
}