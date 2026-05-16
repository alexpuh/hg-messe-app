namespace Herrmann.MesseApp.Server.Tests;

internal static class DbTestHelper
{
    public static (MesseAppDbContext ctx, SqliteConnection connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<MesseAppDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new MesseAppDbContext(options);
        ctx.Database.EnsureCreated();
        return (ctx, connection);
    }
}
