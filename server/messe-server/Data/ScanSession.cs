using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("ScanSessions")]
public class ScanSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public DateTime StartedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public ScanSessionType SessionType { get; set; }
    
    public int? DispatchSheetId { get; set; }
    
    // Navigation Properties
    public DispatchSheet? DispatchSheet { get; set; }
    
    public ICollection<ScannedArticle> ScannedArticles { get; set; } = new List<ScannedArticle>();
}

