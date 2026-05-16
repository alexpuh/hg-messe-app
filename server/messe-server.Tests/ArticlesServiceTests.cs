namespace Herrmann.MesseApp.Server.Tests;

public class ArticlesServiceTests : IDisposable
{
    private readonly MesseAppDbContext _ctx;
    private readonly SqliteConnection _connection;
    private readonly ArticlesService _sut;

    public ArticlesServiceTests()
    {
        (_ctx, _connection) = DbTestHelper.Create();
        _sut = new ArticlesService(_ctx, Substitute.For<ILogger<ArticlesService>>());
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }

    // AC-8: TryFindEan — known EanUnit
    [Fact]
    public void TryFindEan_KnownEanUnit_ReturnsTrueAndArticle()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 1, ArticleId = 10, ArtNr = "ART001",
            Weight = 500, DisplayName = "Article One",
            EanUnit = "1234567890001", EanBox = null
        });
        _ctx.SaveChanges();

        var result = _sut.TryFindEan("1234567890001", out var dto);

        Assert.True(result);
        Assert.NotNull(dto);
        Assert.Equal(1, dto!.UnitId);
    }

    // AC-8: TryFindEan — known EanBox
    [Fact]
    public void TryFindEan_KnownEanBox_ReturnsTrueAndArticle()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 2, ArticleId = 10, ArtNr = "ART001",
            Weight = 500, DisplayName = "Article One",
            EanUnit = "1111111111111", EanBox = "9999999999999"
        });
        _ctx.SaveChanges();

        var result = _sut.TryFindEan("9999999999999", out var dto);

        Assert.True(result);
        Assert.NotNull(dto);
        Assert.Equal(2, dto!.UnitId);
    }

    // AC-8: TryFindEan — unknown EAN
    [Fact]
    public void TryFindEan_UnknownEan_ReturnsFalse()
    {
        var result = _sut.TryFindEan("0000000000000", out var dto);

        Assert.False(result);
        Assert.Null(dto);
    }

    // AC-9: ImportFromJsonFileAsync — valid JSON file
    [Fact]
    public async Task ImportFromJsonFileAsync_ValidFile_ImportsAllUnits()
    {
        var json = """
            [
              {
                "Id": 1, "ArtNr": "A001", "Name1": "Art 1", "Name2": "",
                "DisplayName": "Article One", "IsDisabled": false,
                "Units": [
                  { "Id": 101, "ArticleId": 1, "Weight": 500, "Price": 10.0, "Ean": "1234567890001", "IsDisabled": false, "PackagesInBox": 1 },
                  { "Id": 102, "ArticleId": 1, "Weight": 1000, "Price": 20.0, "Ean": "1234567890002", "IsDisabled": false, "PackagesInBox": 6 }
                ]
              }
            ]
            """;
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, json);
        try
        {
            var count = await _sut.ImportFromJsonFileAsync(path);

            Assert.Equal(2, count);
            Assert.Equal(2, _ctx.ArticleUnits.Count());
        }
        finally
        {
            File.Delete(path);
        }
    }

    // AC-9: ImportFromJsonFileAsync — non-existent file
    [Fact]
    public async Task ImportFromJsonFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _sut.ImportFromJsonFileAsync("/non/existent/path/articles.json"));
    }

    // AC-9: ImportFromJsonFileAsync — empty JSON array
    [Fact]
    public async Task ImportFromJsonFileAsync_EmptyJsonArray_ReturnsZeroAndClearsExistingData()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 1, ArticleId = 1, ArtNr = "A001",
            Weight = 500, DisplayName = "Old Article"
        });
        _ctx.SaveChanges();

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "[]");
        try
        {
            var count = await _sut.ImportFromJsonFileAsync(path);

            Assert.Equal(0, count);
            Assert.Equal(0, _ctx.ArticleUnits.Count());
        }
        finally
        {
            File.Delete(path);
        }
    }

    // TryGetArticleUnit — known unit
    [Fact]
    public void TryGetArticleUnit_KnownUnitId_ReturnsTrueAndDto()
    {
        _ctx.ArticleUnits.Add(new ArticleUnit
        {
            UnitId = 50, ArticleId = 5, ArtNr = "B001",
            Weight = 300, DisplayName = "Article B"
        });
        _ctx.SaveChanges();

        var result = _sut.TryGetArticleUnit(50, out var dto);

        Assert.True(result);
        Assert.NotNull(dto);
        Assert.Equal(50, dto!.UnitId);
    }

    // TryGetArticleUnit — unknown unit
    [Fact]
    public void TryGetArticleUnit_UnknownUnitId_ReturnsFalse()
    {
        var result = _sut.TryGetArticleUnit(9999, out var dto);

        Assert.False(result);
        Assert.Null(dto);
    }

    // GetAllEanUnits — returns only enabled units, both EanUnit and EanBox entries
    [Fact]
    public void GetAllEanUnits_MixedUnits_ReturnsOnlyEnabledEans()
    {
        _ctx.ArticleUnits.AddRange(
            new ArticleUnit
            {
                UnitId = 1, ArticleId = 1, ArtNr = "A001", Weight = 100, DisplayName = "Active",
                EanUnit = "1000000000001", EanBox = "9000000000001",
                IsArticleDisabled = false, IsUnitDisabled = false
            },
            new ArticleUnit
            {
                UnitId = 2, ArticleId = 2, ArtNr = "A002", Weight = 200, DisplayName = "Disabled Unit",
                EanUnit = "1000000000002", EanBox = null,
                IsArticleDisabled = false, IsUnitDisabled = true
            },
            new ArticleUnit
            {
                UnitId = 3, ArticleId = 3, ArtNr = "A003", Weight = 300, DisplayName = "Disabled Article",
                EanUnit = "1000000000003", EanBox = null,
                IsArticleDisabled = true, IsUnitDisabled = false
            }
        );
        _ctx.SaveChanges();

        var result = _sut.GetAllEanUnits();

        // Only UnitId=1 is fully enabled; it contributes EanUnit + EanBox = 2 entries
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Ean == "1000000000001");
        Assert.Contains(result, e => e.Ean == "9000000000001");
    }
}
