using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Data;

public class MesseAppDbContext(DbContextOptions<MesseAppDbContext> options) : DbContext(options)
{
    public DbSet<ArticleUnit> ArticleUnits => Set<ArticleUnit>();
    public DbSet<TradeEvent> TradeEvents => Set<TradeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ArticleUnit>()
            .HasIndex(e => e.EanUnit);
        
        modelBuilder.Entity<ArticleUnit>()
            .HasIndex(e => e.EanBox);
        
        modelBuilder.Entity<ArticleUnit>()
            .HasIndex(e => e.ArticleId);
    }
}

