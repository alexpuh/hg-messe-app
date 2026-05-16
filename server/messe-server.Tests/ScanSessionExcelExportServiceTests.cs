using ClosedXML.Excel;

namespace Herrmann.MesseApp.Server.Tests;

public class ScanSessionExcelExportServiceTests
{
    private readonly ScanSessionExcelExportService _sut;

    public ScanSessionExcelExportServiceTests()
    {
        _sut = new ScanSessionExcelExportService(Substitute.For<ILogger<ScanSessionExcelExportService>>());
    }

    private static List<DtoScanSessionArticle> SampleArticles() =>
    [
        new DtoScanSessionArticle
        {
            Id = 1, UnitId = 101, ArticleNr = "A001", ArticleDisplayName = "Article One",
            UnitWeight = 500, Ean = "1234567890001", Count = 3, RequiredCount = 5
        },
        new DtoScanSessionArticle
        {
            Id = 2, UnitId = 102, ArticleNr = "A002", ArticleDisplayName = "Article Two",
            UnitWeight = 1000, Ean = "1234567890002", Count = 1, RequiredCount = null
        }
    ];

    // AC-10: Generate — showExpectation = true → 7 columns
    [Fact]
    public void Generate_ShowExpectationTrue_WorksheetContains7HeaderColumns()
    {
        using var stream = new MemoryStream();
        _sut.Generate(stream, "Test Sheet", SampleArticles(), "TestTab", showExpectation: true, title: "Test Title");

        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var headerRow = ws.Row(3);

        Assert.Equal("Art.Nr.", headerRow.Cell(1).GetString());
        Assert.Equal("Artikel", headerRow.Cell(2).GetString());
        Assert.Equal("Gewicht", headerRow.Cell(3).GetString());
        Assert.Equal("EAN", headerRow.Cell(4).GetString());
        Assert.Equal("Bestand", headerRow.Cell(5).GetString());
        Assert.Equal("Soll", headerRow.Cell(6).GetString());
        Assert.Equal("Fehlt", headerRow.Cell(7).GetString());
    }

    // AC-10: Generate — showExpectation = false → 5 columns only (no Soll/Fehlt)
    [Fact]
    public void Generate_ShowExpectationFalse_WorksheetContains5HeaderColumnsOnly()
    {
        using var stream = new MemoryStream();
        _sut.Generate(stream, "Test Sheet", SampleArticles(), "TestTab", showExpectation: false, title: "Test Title");

        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var headerRow = ws.Row(3);

        Assert.Equal("Art.Nr.", headerRow.Cell(1).GetString());
        Assert.Equal("Artikel", headerRow.Cell(2).GetString());
        Assert.Equal("Gewicht", headerRow.Cell(3).GetString());
        Assert.Equal("EAN", headerRow.Cell(4).GetString());
        Assert.Equal("Bestand", headerRow.Cell(5).GetString());
        Assert.True(string.IsNullOrEmpty(headerRow.Cell(6).GetString()));
        Assert.True(string.IsNullOrEmpty(headerRow.Cell(7).GetString()));
    }

    // AC-11: GenerateCombined → worksheet named "Messeabschluss", 9 columns
    [Fact]
    public void GenerateCombined_ValidInput_WorksheetNamedMesseabschlussWithNineColumns()
    {
        var standSession = new ScanSession
        {
            Id = 1, StartedAt = new DateTime(2024, 6, 10, 10, 0, 0), UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Stand
        };
        var lagerSession = new ScanSession
        {
            Id = 2, StartedAt = new DateTime(2024, 6, 10, 14, 0, 0), UpdatedAt = DateTime.Now,
            SessionType = ScanSessionType.Inventory, Ort = Ort.Lager
        };
        var articles = new List<DtoCombinedArticle>
        {
            new DtoCombinedArticle
            {
                UnitId = 1, ArticleNr = "A001", ArticleDisplayName = "Article One",
                UnitWeight = 500, Ean = "1111111111111",
                CountStand = 3, CountAnhaenger = 2, Total = 5,
                RequiredCount = 8, Fehlt = 3
            }
        };

        using var stream = new MemoryStream();
        _sut.GenerateCombined(stream, standSession, lagerSession, articles);

        using var workbook = new XLWorkbook(stream);
        Assert.Single(workbook.Worksheets);

        var ws = workbook.Worksheets.First();
        Assert.Equal("Messeabschluss", ws.Name);

        var headerRow = ws.Row(3);
        Assert.Equal("Art.Nr.", headerRow.Cell(1).GetString());
        Assert.Equal("Artikel", headerRow.Cell(2).GetString());
        Assert.Equal("Gewicht", headerRow.Cell(3).GetString());
        Assert.Equal("EAN", headerRow.Cell(4).GetString());
        Assert.Equal("Gesamt", headerRow.Cell(7).GetString());
        Assert.Equal("Soll", headerRow.Cell(8).GetString());
        Assert.Equal("Fehlt", headerRow.Cell(9).GetString());
    }
}
