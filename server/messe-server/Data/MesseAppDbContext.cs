using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Data;

public class MesseAppDbContext(DbContextOptions<MesseAppDbContext> options) : DbContext(options)
{
    public DbSet<ArticleUnit> ArticleUnits => Set<ArticleUnit>();
    public DbSet<TradeEvent> TradeEvents => Set<TradeEvent>();
    public DbSet<TradeEventRequiredUnit> TradeEventRequiredUnits => Set<TradeEventRequiredUnit>();

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
    }
}

