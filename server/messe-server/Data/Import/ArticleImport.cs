using JetBrains.Annotations;

namespace Herrmann.MesseApp.Server.Data.Import;

[UsedImplicitly]
public class ArticleImport
{
    public int Id { get; set; }
    public string ArtNr { get; set; } = string.Empty;
    public string Name1 { get; set; } = string.Empty;
    public string Name2 { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public List<ArticleUnitImport> Units { get; set; } = new();
}

[UsedImplicitly]
public class ArticleUnitImport
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public int Weight { get; set; }
    public decimal Price { get; set; }
    public string Ean { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public int PackagesInBox { get; set; }
}

