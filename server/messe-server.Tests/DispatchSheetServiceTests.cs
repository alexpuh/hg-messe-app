namespace Herrmann.MesseApp.Server.Tests;

public class DispatchSheetServiceTests : IDisposable
{
    private readonly MesseAppDbContext _ctx;
    private readonly SqliteConnection _connection;
    private readonly DispatchSheetService _sut;

    public DispatchSheetServiceTests()
    {
        (_ctx, _connection) = DbTestHelper.Create();
        _sut = new DispatchSheetService(_ctx, Substitute.For<ILogger<DispatchSheetService>>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }

    // ─── AC-6: CRUD ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewSheet_PersistsAndReturnsWithValidId()
    {
        var result = await _sut.AddAsync(new DtoDispatchSheet { Name = "Test Sheet" });

        Assert.True(result.Id > 0);
        Assert.Equal("Test Sheet", result.Name);
        Assert.True(_ctx.DispatchSheets.Any(d => d.Id == result.Id));
    }

    [Fact]
    public async Task UpdateAsync_ExistingSheet_UpdatesNameAndReturnsTrue()
    {
        var entity = new DispatchSheet { Name = "Old Name" };
        _ctx.DispatchSheets.Add(entity);
        _ctx.SaveChanges();

        var result = await _sut.UpdateAsync(entity.Id, new DtoDispatchSheet { Id = entity.Id, Name = "New Name" });

        Assert.True(result);
        var updated = _ctx.DispatchSheets.Find(entity.Id);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsFalse()
    {
        var result = await _sut.UpdateAsync(9999, new DtoDispatchSheet { Id = 9999, Name = "Name" });

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingSheet_RemovesAndReturnsTrue()
    {
        var entity = new DispatchSheet { Name = "To Delete" };
        _ctx.DispatchSheets.Add(entity);
        _ctx.SaveChanges();

        var result = await _sut.DeleteAsync(entity.Id);

        Assert.True(result);
        Assert.False(_ctx.DispatchSheets.Any(d => d.Id == entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(9999);

        Assert.False(result);
    }

    // ─── AC-7: SetRequiredUnitsAsync ──────────────────────────────────────────

    [Fact]
    public async Task SetRequiredUnitsAsync_ValidInputs_PersistsRequiredCount()
    {
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        var result = await _sut.SetRequiredUnitsAsync(sheet.Id, 1, 10);

        Assert.True(result);
        var record = _ctx.DispatchSheetRequiredUnits
            .FirstOrDefault(r => r.DispatchSheetId == sheet.Id && r.UnitId == 1);
        Assert.NotNull(record);
        Assert.Equal(10, record!.RequiredCount);
    }

    [Fact]
    public async Task SetRequiredUnitsAsync_SameUnitIdAgain_UpdatesExistingRecordWithoutDuplicate()
    {
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        await _sut.SetRequiredUnitsAsync(sheet.Id, 1, 10);
        await _sut.SetRequiredUnitsAsync(sheet.Id, 1, 25);

        var records = _ctx.DispatchSheetRequiredUnits
            .Where(r => r.DispatchSheetId == sheet.Id && r.UnitId == 1)
            .ToList();
        Assert.Single(records);
        Assert.Equal(25, records[0].RequiredCount);
    }

    [Fact]
    public async Task SetRequiredUnitsAsync_CountZero_ThrowsArgumentOutOfRangeException()
    {
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.SetRequiredUnitsAsync(sheet.Id, 1, 0));
    }

    [Fact]
    public async Task SetRequiredUnitsAsync_CountNegative_ThrowsArgumentOutOfRangeException()
    {
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.SetRequiredUnitsAsync(sheet.Id, 1, -5));
    }

    [Fact]
    public async Task SetRequiredUnitsAsync_NonExistentDispatchSheet_ReturnsFalse()
    {
        var result = await _sut.SetRequiredUnitsAsync(9999, 1, 5);

        Assert.False(result);
    }

    // ─── DeleteRequiredUnitAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteRequiredUnitAsync_ExistingEntry_RemovesIt()
    {
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        await _sut.SetRequiredUnitsAsync(sheet.Id, 1, 5);
        await _sut.DeleteRequiredUnitAsync(sheet.Id, 1);

        Assert.False(_ctx.DispatchSheetRequiredUnits
            .Any(r => r.DispatchSheetId == sheet.Id && r.UnitId == 1));
    }

    [Fact]
    public async Task DeleteRequiredUnitAsync_NonExistingEntry_CompletesWithoutError()
    {
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        // Should not throw
        await _sut.DeleteRequiredUnitAsync(sheet.Id, 999);
    }

    // ─── GetDispatchSheetArticleUnits ─────────────────────────────────────────

    [Fact]
    public async Task GetDispatchSheetArticleUnits_ExistingSheet_ReturnsAllEnabledArticlesWithRequiredCount()
    {
        _ctx.ArticleUnits.AddRange(
            new ArticleUnit { UnitId = 1, ArticleId = 1, ArtNr = "A001", DisplayName = "Art One", Weight = 100, EanUnit = "1111" },
            new ArticleUnit { UnitId = 2, ArticleId = 2, ArtNr = "A002", DisplayName = "Art Two", Weight = 200, EanUnit = "2222" },
            new ArticleUnit { UnitId = 3, ArticleId = 3, ArtNr = "A003", DisplayName = "Art Three (disabled)", Weight = 300, IsArticleDisabled = true }
        );
        var sheet = new DispatchSheet { Name = "Sheet" };
        _ctx.DispatchSheets.Add(sheet);
        _ctx.SaveChanges();

        await _sut.SetRequiredUnitsAsync(sheet.Id, 1, 8);

        var result = await _sut.GetDispatchSheetArticleUnits(sheet.Id);

        Assert.NotNull(result);
        // Disabled article is excluded
        Assert.Equal(2, result!.Length);
        var withRequired = result.First(a => a.UnitId == 1);
        Assert.Equal(8, withRequired.RequiredCount);
        var withoutRequired = result.First(a => a.UnitId == 2);
        Assert.Null(withoutRequired.RequiredCount);
    }

    [Fact]
    public async Task GetDispatchSheetArticleUnits_NonExistentSheet_ReturnsNull()
    {
        var result = await _sut.GetDispatchSheetArticleUnits(9999);

        Assert.Null(result);
    }
}
