namespace Herrmann.MesseApp.Server.Dto;

public class DtoScanSessionArticle
{
    public required int Id { get; init; }
    public required int UnitId { get; init; }
    public required string ArticleNr { get; init; }
    public required string ArticleDisplayName { get; init; }
    public int UnitWeight { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required string Ean { get; init; }
    public int Count { get; init; }
    public int? RequiredCount { get; init; }
}