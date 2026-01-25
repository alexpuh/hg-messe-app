using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("BarcodeScans")]
public class BarcodeScan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Ean { get; set; } = string.Empty;
    
    public DateTime ScannedAt { get; set; }
    
    public int StockItemId { get; set; }
    
    // Navigation Property
    public StockItem? StockItem { get; set; }
}

