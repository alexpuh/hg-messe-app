using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

/// <summary>
/// Verknüpfung zwischen LoadingList und ArticleUnit mit erforderlicher Menge
/// </summary>
[Table("LoadingListRequiredUnits")]
public class LoadingListRequiredUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// ID der Beladeliste
    /// </summary>
    public int LoadingListId { get; set; }
    
    /// <summary>
    /// ID der Artikeleinheit
    /// </summary>
    public int UnitId { get; set; }
    
    /// <summary>
    /// Erforderliche Anzahl dieser Einheiten für die Beladeliste
    /// </summary>
    public int RequiredCount { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    public LoadingList? LoadingList { get; set; }
}

