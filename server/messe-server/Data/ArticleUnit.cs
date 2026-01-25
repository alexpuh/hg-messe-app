using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("ArticleUnits")]
public class ArticleUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int UnitId { get; set; }
    
    public int ArticleId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ArtNr { get; set; } = string.Empty;
    
    public int Weight { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    public bool IsArticleDisabled { get; set; }
    
    public bool IsUnitDisabled { get; set; }
    
    public int PackagesInBox { get; set; }
    
    [MaxLength(50)]
    public string? EanUnit { get; set; }
    
    [MaxLength(50)]
    public string? EanBox { get; set; }
}

