using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herrmann.MesseApp.Server.Data;

[Table("DispatchSheets")]
public class DispatchSheet
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public ICollection<DispatchSheetRequiredUnit> RequiredUnits { get; set; } = new List<DispatchSheetRequiredUnit>();
}

