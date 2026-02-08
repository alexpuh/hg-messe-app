namespace Herrmann.MesseApp.Server.Dto;

public class DtoScanSession
{
    public int? Id { get; set; }
    public DateTime? StartedAt { get; set; }
    public int? DispatchSheetId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}