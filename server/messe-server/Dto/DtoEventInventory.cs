namespace Herrmann.MesseApp.Server.Dto;

public class DtoEventInventory
{
    public int? Id { get; set; }
    public DateTime? StartedAt { get; set; }
    public int? TradeEventId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}