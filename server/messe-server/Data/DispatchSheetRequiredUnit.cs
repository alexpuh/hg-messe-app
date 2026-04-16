using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

/// <summary>
/// Verknüpfung zwischen DispatchSheet und ArticleUnit mit erforderlicher Menge
/// </summary>
[Table("DispatchSheetRequiredUnits")]
public class DispatchSheetRequiredUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// ID der Verladeschein
    /// </summary>
    public int DispatchSheetId { get; set; }
    
    /// <summary>
    /// ID der Artikeleinheit
    /// </summary>
    public int UnitId { get; set; }
    
    /// <summary>
    /// Erforderliche Anzahl dieser Einheiten für die Verladeschein
    /// </summary>
    public int RequiredCount { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    public DispatchSheet? DispatchSheet { get; set; }
}

