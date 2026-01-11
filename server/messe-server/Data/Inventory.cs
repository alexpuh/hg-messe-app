using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("Inventories")]
public class Inventory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public DateTime StartedAt { get; set; }
    
    public int? TradeEventId { get; set; }
    
    // Navigation Properties
    public TradeEvent? TradeEvent { get; set; }
    
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}

