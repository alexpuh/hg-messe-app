using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

/// <summary>
/// Verknüpfung zwischen TradeEvent und ArticleUnit mit erforderlicher Menge
/// </summary>
[Table("TradeEventRequiredUnits")]
public class TradeEventRequiredUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// ID des Trade Events
    /// </summary>
    public int TradeEventId { get; set; }
    
    /// <summary>
    /// ID der Artikeleinheit
    /// </summary>
    public int UnitId { get; set; }
    
    /// <summary>
    /// Erforderliche Anzahl dieser Einheiten für das Event
    /// </summary>
    public int RequiredCount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    // Navigation Property
    public TradeEvent? TradeEvent { get; set; }
}

