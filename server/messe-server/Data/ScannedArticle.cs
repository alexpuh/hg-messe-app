using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("ScannedArticles")]
public class ScannedArticle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int ScanSessionId { get; set; }
    
    public int UnitId { get; set; }
    
    public int QuantityUnits { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public ScanSession? ScanSession { get; set; }
    
    public ICollection<BarcodeScan> BarcodeScans { get; set; } = new List<BarcodeScan>();
}

