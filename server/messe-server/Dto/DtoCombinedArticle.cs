namespace Herrmann.MesseApp.Server.Dto;

public class DtoCombinedArticle
{
    public required int UnitId { get; init; }
    public required string? ArticleNr { get; init; }
    public required string? ArticleDisplayName { get; init; }
    public required int UnitWeight { get; init; }
    public required string? Ean { get; init; }
    public required int CountStand { get; init; }
    public required int CountAnhaenger { get; init; }
    public required int Total { get; init; }
    public required int? RequiredCount { get; init; }
    public required int? Fehlt { get; init; }
}
