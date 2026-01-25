using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("StockItems")]
public class StockItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int InventoryId { get; set; }
    
    public int UnitId { get; set; }
    
    public int QuantityUnits { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public Inventory? Inventory { get; set; }
    
    public ICollection<BarcodeScan> BarcodeScans { get; set; } = new List<BarcodeScan>();
}

