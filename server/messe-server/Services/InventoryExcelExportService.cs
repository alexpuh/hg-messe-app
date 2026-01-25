using ClosedXML.Excel;
using Herrmann.MesseApp.Server.Dto;

namespace Herrmann.MesseApp.Server.Services;

public class InventoryExcelExportService(ILogger<InventoryExcelExportService> logger)
{
    public void Generate(Stream stream, IEnumerable<DtoInventoryStockItem> inventory, string workbookName)
    {
        logger.LogDebug("Excel Export started: {WorkbookName}", workbookName);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(workbookName);
        GenerateExcel(inventory.OrderBy(i => i.ArticleNr), ws);
        workbook.SaveAs(stream);
    }

    private static void GenerateExcel(IEnumerable<DtoInventoryStockItem> items, IXLWorksheet ws)
    {
        var zeile = 1;

        ws.Cell(zeile, 1).Value = "Art.Nr.";
        ws.Cell(zeile, 2).Value = "Artikel";
        ws.Cell(zeile, 3).Value = "Gewicht";
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(zeile, 4).Value = "EAN";
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(zeile, 5).Value = "Bestand";
        ws.Cell(zeile, 6).Value = "Soll";
        ws.Cell(zeile, 7).Value = "Differenz";

        foreach (var item in items)
        {
            zeile++;
            ws.Cell(zeile, 1).Value = $"{item.ArticleNr}";
            ws.Cell(zeile, 2).Value = $"{item.ArticleDisplayName}";
            ws.Cell(zeile, 3).Value = $"{item.UnitWeight}";
            ws.Cell(zeile, 4).SetValue(item.Ean);
            ws.Cell(zeile, 5).Value = $"{item.Count}";
            ws.Cell(zeile, 6).Value = $"{item.RequiredCount}";
            ws.Cell(zeile, 7).Value = $"{item.Count - item.RequiredCount}";
        }
    }
} 