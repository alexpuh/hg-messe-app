namespace Herrmann.MesseApp.Server.Dto;

public class DtoInventoryStockItem
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string? ArticleNr { get; set; }
    public string? ArticleDisplayName { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Ean { get; set; }
    public int Count { get; set; }
    public int? RequiredCount { get; set; }
}