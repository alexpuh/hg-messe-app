namespace Herrmann.MesseApp.Server.Tests;

public class ScanSessionServiceTests : IDisposable
{
    private readonly MesseAppDbContext _ctx;
    private readonly SqliteConnection _connection;
    private readonly ScanSessionService _sut;

    public ScanSessionServiceTests()
    {
        (_ctx, _connection) = DbTestHelper.Create();
        var articlesService = new ArticlesService(_ctx, Substitute.For<ILogger<ArticlesService>>());
        _sut = new ScanSessionService(_ctx, articlesService, Substitute.For<ILogger<ScanSessionService>>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }

    // ─── AC-2: CreateScanSessionAsync invariants ───────────────────────────────

    [Fact]
    public async Task CreateScanSessionAsync_ProcessDispatchList_OrtStand_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateScanSessionAsync(ScanSessionType.ProcessDispatchList, Ort.Stand, 1));
    }

    [Fact]
    public async Task CreateScanSessionAsync_ProcessDispatchList_NoDispatchSheetId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateScanSessionAsync(ScanSessionType.ProcessDispatchList, Ort.Lager, null));
    }

    [Fact]
    public async Task CreateScanSessionAsync_Inventory_Lager_NoDispatchSheetId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateScanSessionAsync(ScanSessionType.Inventory, Ort.Lager, null));
    }

    [Fact]
    public async Task CreateScanSessionAsync_Inventory_Stand_WithDispatchSheetId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateScanSessionAsync(ScanSessionType.Inventory, Ort.Stand, 1));
    }

    [Fact]
    public async Task CreateScanSessionAsync_ProcessDispatchList_Lager_WithDispatchSheetId_PersistsAndReturnsId()
    {
        var sheet = new DispatchSheet { Name = "Sheet 1" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        var id = await _sut.CreateScanSessionAsync(ScanSessionType.ProcessDispatchList, Ort.Lager, sheet.Id);

        Assert.True(id > 0);
        Assert.True(await _ctx.ScanSessions.AnyAsync(s => s.Id == id));
    }

    [Fact]
    public async Task CreateScanSessionAsync_Inventory_Stand_NoDispatchSheetId_PersistsAndReturnsId()
    {
        var id = await _sut.CreateScanSessionAsync(ScanSessionType.Inventory, Ort.Stand, null);

        Assert.True(id > 0);
        Assert.True(await _ctx.ScanSessions.AnyAsync(s => s.Id == id));
    }

    [Fact]
    public async Task CreateScanSessionAsync_Inventory_Lager_WithDispatchSheetId_PersistsAndReturnsId()
    {
        var sheet = new DispatchSheet { Name = "Sheet 2" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        var id = await _sut.CreateScanSessionAsync(ScanSessionType.Inventory, Ort.Lager, sheet.Id);

        Assert.True(id > 0);
        Assert.True(await _ctx.ScanSessions.AnyAsync(s => s.Id == id));
    }

    // ─── AC-3: AddBarcodeAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task AddBarcodeAsync_KnownEan_ReturnsSuccessAndCreatesScannedArticle()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 101, ArticleId = 1, ArtNr = "A001",
            DisplayName = "Art One", Weight = 500, EanUnit = "1234567890001"
        });
        var session = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        _ctx.ScanSessions.Add(session);
        _ctx.SaveChanges();

        var (success, error) = await _sut.AddBarcodeAsync(session.Id, "1234567890001");

        Assert.True(success);
        Assert.Equal(string.Empty, error);
        var scanned = _ctx.ScannedArticles.FirstOrDefault(a => a.ScanSessionId == session.Id && a.UnitId == 101);
        Assert.NotNull(scanned);
        Assert.Equal(1, scanned!.QuantityUnits);
    }

    [Fact]
    public async Task AddBarcodeAsync_SameEanScannedTwice_IncrementsQuantityUnitsWithoutDuplicate()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 102, ArticleId = 1, ArtNr = "A002",
            DisplayName = "Art Two", Weight = 200, EanUnit = "2222222222222"
        });
        var session = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        _ctx.ScanSessions.Add(session);
        _ctx.SaveChanges();

        await _sut.AddBarcodeAsync(session.Id, "2222222222222");
        _ctx.ChangeTracker.Clear(); // reload from DB on second call to verify persisted behaviour
        await _sut.AddBarcodeAsync(session.Id, "2222222222222");
        _ctx.ChangeTracker.Clear();

        var scanned = _ctx.ScannedArticles.Where(a => a.ScanSessionId == session.Id && a.UnitId == 102).ToList();
        Assert.Single(scanned);
        Assert.Equal(2, scanned[0].QuantityUnits);
    }

    [Fact]
    public async Task AddBarcodeAsync_UnknownEan_ReturnsFalseWithEanInMessage()
    {
        var session = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        _ctx.ScanSessions.Add(session);
        _ctx.SaveChanges();

        var (success, error) = await _sut.AddBarcodeAsync(session.Id, "0000000000000");

        Assert.False(success);
        Assert.Contains("0000000000000", error);
    }

    [Fact]
    public async Task AddBarcodeAsync_NonExistentSession_ReturnsFalseWithSessionIdInMessage()
    {
        var (success, error) = await _sut.AddBarcodeAsync(9999, "1234567890001");

        Assert.False(success);
        Assert.Contains("9999", error);
    }

    // ─── AC-4: GetScanSessionArticlesAsync ────────────────────────────────────

    [Fact]
    public async Task GetScanSessionArticlesAsync_WithScannedAndRequiredArticles_ReturnsBoth()
    {
        _ctx.ArticleUnits.AddRange(
            new ArticleUnit { UnitId = 1, ArticleId = 1, ArtNr = "A001", DisplayName = "Art One", Weight = 100, EanUnit = "1111111111111" },
            new ArticleUnit { UnitId = 2, ArticleId = 2, ArtNr = "A002", DisplayName = "Art Two", Weight = 200, EanUnit = "2222222222222" }
        );
        var sheet = new DispatchSheet { Name = "Sheet A" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        _ctx.DispatchSheetRequiredUnits.Add(new DispatchSheetRequiredUnit
        {
            DispatchSheetId = sheet.Id, UnitId = 2, RequiredCount = 5
        });
        var session = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.ProcessDispatchList, Ort = Ort.Lager,
            DispatchSheetId = sheet.Id
        };
        _ctx.ScanSessions.Add(session);
        _ctx.SaveChanges();

        _ctx.ScannedArticles.Add(new ScannedArticle
        {
            ScanSessionId = session.Id, UnitId = 1, QuantityUnits = 3, UpdatedAt = DateTime.Now
        });
        _ctx.SaveChanges();

        var result = await _sut.GetScanSessionArticlesAsync(session.Id);

        Assert.NotNull(result);
        var articles = result!.Value.articles;
        Assert.Equal(2, articles.Length);

        var scanned = articles.First(a => a.UnitId == 1);
        Assert.Equal(3, scanned.Count);

        var unscanned = articles.First(a => a.UnitId == 2);
        Assert.Equal(0, unscanned.Count);
        Assert.Equal(5, unscanned.RequiredCount);
    }

    [Fact]
    public async Task GetScanSessionArticlesAsync_EmptySessionWithNoDispatchSheet_ReturnsEmptyArray()
    {
        var session = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        _ctx.ScanSessions.Add(session);
        _ctx.SaveChanges();

        var result = await _sut.GetScanSessionArticlesAsync(session.Id);

        Assert.NotNull(result);
        Assert.Empty(result!.Value.articles);
    }

    [Fact]
    public async Task GetScanSessionArticlesAsync_NonExistentSession_ReturnsNull()
    {
        var result = await _sut.GetScanSessionArticlesAsync(9999);

        Assert.Null(result);
    }

    // ─── AC-5: GetCombinedArticlesAsync ───────────────────────────────────────

    [Fact]
    public async Task GetCombinedArticlesAsync_ValidSessions_MergesArticleCountsCorrectly()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 1, ArticleId = 1, ArtNr = "A001", DisplayName = "Art One", Weight = 100, EanUnit = "1111111111111"
        });
        var standSession = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        var lagerSession = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Lager
        };
        _ctx.ScanSessions.AddRange(standSession, lagerSession);
        _ctx.SaveChanges();

        _ctx.ScannedArticles.AddRange(
            new ScannedArticle { ScanSessionId = standSession.Id, UnitId = 1, QuantityUnits = 3, UpdatedAt = DateTime.Now },
            new ScannedArticle { ScanSessionId = lagerSession.Id, UnitId = 1, QuantityUnits = 2, UpdatedAt = DateTime.Now }
        );
        _ctx.SaveChanges();

        var result = await _sut.GetCombinedArticlesAsync(standSession.Id, lagerSession.Id);

        Assert.NotNull(result);
        var combined = result!.Value.articles;
        Assert.Single(combined);
        Assert.Equal(3, combined[0].CountStand);
        Assert.Equal(2, combined[0].CountAnhaenger);
        Assert.Equal(5, combined[0].Total);
    }

    [Fact]
    public async Task GetCombinedArticlesAsync_StandSessionIsActuallyLager_ReturnsNull()
    {
        var wrongSession = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Lager
        };
        var lagerSession = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Lager
        };
        _ctx.ScanSessions.AddRange(wrongSession, lagerSession);
        _ctx.SaveChanges();

        var result = await _sut.GetCombinedArticlesAsync(wrongSession.Id, lagerSession.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCombinedArticlesAsync_LagerSessionNotFound_ReturnsNull()
    {
        var standSession = new ScanSession
        {
            StartedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        _ctx.ScanSessions.Add(standSession);
        _ctx.SaveChanges();

        var result = await _sut.GetCombinedArticlesAsync(standSession.Id, 9999);

        Assert.Null(result);
    }
}
