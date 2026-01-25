using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("TradeEvents")]
public class TradeEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation Property: Ein TradeEvent kann mehrere Required Units haben
    public ICollection<TradeEventRequiredUnit> RequiredUnits { get; set; } = new List<TradeEventRequiredUnit>();
}

