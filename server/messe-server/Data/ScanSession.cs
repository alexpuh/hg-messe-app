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
    
    public DateTime? UpdatedAt { get; set; }
    
    public int? LoadingListId { get; set; }
    
    // Navigation Properties
    public LoadingList? LoadingList { get; set; }
    
    public ICollection<ScannedArticle> ScannedArticles { get; set; } = new List<ScannedArticle>();
}

