using Herrmann.MesseApp.Server.Data;

namespace Herrmann.MesseApp.Server.Dto;

public class DtoScanSession
{
    public required int Id { get; init; }
    public required DateTime StartedAt { get; init; }
    public required ScanSessionType SessionType { get; init; }
    public required Ort Ort { get; init; }
    public required int? DispatchSheetId { get; init; }
    public required DateTime UpdatedAt { get; set; }
}