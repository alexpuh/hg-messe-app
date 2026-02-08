namespace Herrmann.MesseApp.Server.Dto;

public class DtoLoadingLIstArticleUnit
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string? ArticleNr { get; set; }
    public string? ArticleDisplayName { get; set; }
    public int UnitWeight { get; set; }
    public string Ean { get; set; }
    public int? RequiredCount { get; set; }
}