namespace Herrmann.MesseApp.Server.Dto;

public class DtoArticleUnit
{
    public int UnitId { get; set; }
    public required string ArticleNr { get; set; }
    public required string ArticleName { get; set; }
    public int? Gewicht { get; set; }
    public string? EanUnit { get; set; }
    public string? EanBox { get; set; }
    public int? UnitsPerBox { get; set; }
}