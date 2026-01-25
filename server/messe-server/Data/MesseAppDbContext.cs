using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Data;

public class MesseAppDbContext(DbContextOptions<MesseAppDbContext> options) : DbContext(options)
{
    public DbSet<ArticleUnit> ArticleUnits => Set<ArticleUnit>();
    public DbSet<TradeEvent> TradeEvents => Set<TradeEvent>();
    public DbSet<TradeEventRequiredUnit> TradeEventRequiredUnits => Set<TradeEventRequiredUnit>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
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
        
        // TradeEventRequiredUnit Indizes
        modelBuilder.Entity<TradeEventRequiredUnit>()
            .HasIndex(e => e.TradeEventId);
        
        modelBuilder.Entity<TradeEventRequiredUnit>()
            .HasIndex(e => new { e.TradeEventId, e.UnitId })
            .IsUnique(); // Ein UnitId kann nur einmal pro TradeEvent vorkommen
        
        // Relation: TradeEvent -> TradeEventRequiredUnits (1:n)
        modelBuilder.Entity<TradeEvent>()
            .HasMany(t => t.RequiredUnits)
            .WithOne(r => r.TradeEvent)
            .HasForeignKey(r => r.TradeEventId)
            .OnDelete(DeleteBehavior.Cascade); // Beim Löschen eines TradeEvents werden auch die RequiredUnits gelöscht
        
        // Inventory Indizes
        modelBuilder.Entity<Inventory>()
            .HasIndex(e => e.TradeEventId);
        
        // Relation: Inventory -> StockItems (1:n)
        modelBuilder.Entity<Inventory>()
            .HasMany(i => i.StockItems)
            .WithOne(s => s.Inventory)
            .HasForeignKey(s => s.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Relation: Inventory -> TradeEvent (n:1)
        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.TradeEvent)
            .WithMany()
            .HasForeignKey(i => i.TradeEventId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // StockItem Indizes
        modelBuilder.Entity<StockItem>()
            .HasIndex(e => e.InventoryId);
        
        modelBuilder.Entity<StockItem>()
            .HasIndex(e => e.UnitId);
        
        // Relation: StockItem -> BarcodeScans (1:n)
        modelBuilder.Entity<StockItem>()
            .HasMany(s => s.BarcodeScans)
            .WithOne(b => b.StockItem)
            .HasForeignKey(b => b.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // BarcodeScan Indizes
        modelBuilder.Entity<BarcodeScan>()
            .HasIndex(e => e.StockItemId);
        
        modelBuilder.Entity<BarcodeScan>()
            .HasIndex(e => e.ScannedAt);
    }
}

