namespace Herrmann.MesseApp.Server.Dto;

public class DtoStockItem
{
    public int UnitId { get; set; }
    public string ArticleNr { get; set; }
    public string ArticleDisplayName { get; set; }
    public int QuantityUnits { get; set; }
    public int QuantityBox { get; set; }
    public int QuantityPerBox { get; set; }
    public int? Required { get; set; }
    public DateTime updatedAt { get; set; }
}