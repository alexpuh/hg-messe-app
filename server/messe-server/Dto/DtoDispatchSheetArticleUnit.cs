namespace Herrmann.MesseApp.Server.Dto;

public class DtoDispatchSheetArticleUnit
{
    public required int Id { get; init; }
    public required int UnitId { get; init; }
    public required string? ArticleNr { get; init; }
    public required string? ArticleDisplayName { get; init; }
    public required int UnitWeight { get; init; }
    public required string Ean { get; init; }
    public required int? RequiredCount { get; init; }
}