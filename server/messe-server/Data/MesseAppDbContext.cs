using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Data;

public class MesseAppDbContext(DbContextOptions<MesseAppDbContext> options) : DbContext(options)
{
    public DbSet<ArticleUnit> ArticleUnits => Set<ArticleUnit>();
    public DbSet<LoadingList> LoadingLists => Set<LoadingList>();
    public DbSet<LoadingListRequiredUnit> LoadingListRequiredUnits => Set<LoadingListRequiredUnit>();
    public DbSet<ScanSession> ScanSessions => Set<ScanSession>();
    public DbSet<ScannedArticle> ScannedArticles => Set<ScannedArticle>();
    public DbSet<BarcodeScan> BarcodeScans => Set<BarcodeScan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ArticleUnit>()
            .HasIndex(e => e.EanUnit);
        
        modelBuilder.Entity<ArticleUnit>()
            .HasIndex(e => e.EanBox);
        
        modelBuilder.Entity<ArticleUnit>()
            .HasIndex(e => e.ArticleId);
        
        modelBuilder.Entity<LoadingListRequiredUnit>()
            .HasIndex(e => e.LoadingListId);
        
        modelBuilder.Entity<LoadingListRequiredUnit>()
            .HasIndex(e => new { e.LoadingListId, e.UnitId })
            .IsUnique();
        
        modelBuilder.Entity<LoadingList>()
            .HasMany(t => t.RequiredUnits)
            .WithOne(r => r.LoadingList)
            .HasForeignKey(r => r.LoadingListId)
            .OnDelete(DeleteBehavior.Cascade); // Beim Löschen einer Beladeliste werden auch die RequiredUnits gelöscht
        
        modelBuilder.Entity<ScanSession>()
            .HasIndex(e => e.LoadingListId);
        
        modelBuilder.Entity<ScanSession>()
            .HasMany(i => i.ScannedArticles)
            .WithOne(s => s.ScanSession)
            .HasForeignKey(s => s.ScanSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ScanSession>()
            .HasOne(i => i.LoadingList)
            .WithMany()
            .HasForeignKey(i => i.LoadingListId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<ScannedArticle>()
            .HasIndex(e => e.ScanSessionId);
        
        modelBuilder.Entity<ScannedArticle>()
            .HasIndex(e => e.UnitId);
        
        modelBuilder.Entity<ScannedArticle>()
            .HasMany(s => s.BarcodeScans)
            .WithOne(b => b.StockItem)
            .HasForeignKey(b => b.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<BarcodeScan>()
            .HasIndex(e => e.StockItemId);
        
        modelBuilder.Entity<BarcodeScan>()
            .HasIndex(e => e.ScannedAt);
    }
}

