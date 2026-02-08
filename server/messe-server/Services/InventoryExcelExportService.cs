using ClosedXML.Excel;
using Herrmann.MesseApp.Server.Dto;

namespace Herrmann.MesseApp.Server.Services;

public class InventoryExcelExportService(ILogger<InventoryExcelExportService> logger)
{
    public void Generate(Stream stream, string? tradeEventName, IEnumerable<DtoInventoryStockItem> inventory, string workbookName)
    {
        logger.LogDebug("Excel Export started: {WorkbookName}", workbookName);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(workbookName);
        GenerateExcel(tradeEventName, inventory.OrderBy(i => i.ArticleNr), ws);
        workbook.SaveAs(stream);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    private static void GenerateExcel(string? tradeEventName, IEnumerable<DtoInventoryStockItem> items, IXLWorksheet ws)
    {
        const int maxCol = 7;
        ws.Column(1).Width = 10;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 10;
        ws.Column(6).Width = 10;
        ws.Column(7).Width = 10;
        
        var zeile = 1;
        ws.Row(zeile).Height = 30;
        ws.Range(zeile, 1, zeile, maxCol).Merge();
        ws.Cell(zeile, 1).Value = "LKW-Kontrolle";
        SetHeaderStyle(ws.Cell(zeile, 1));

        zeile++;
        ws.Row(zeile).Height = 25;
        ws.Range(zeile, 1, zeile, 4).Merge();
        SetCell(ws, zeile, 1, cell =>
        {
            cell.Value = tradeEventName;
            SetHeader2Style(cell, XLAlignmentHorizontalValues.Left);
        });
        
        ws.Range(zeile, 5, zeile, maxCol).Merge();
        SetCell(ws, zeile, 5, cell =>
        {
            cell.Value = DateTime.Today;
            SetHeader2Style(cell, XLAlignmentHorizontalValues.Right);
        });

        zeile++;
        ws.Row(zeile).Height = 45;

        SetCell(ws, zeile, 1, cell =>
        {
            cell.Value = "Art.Nr.";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Left);
        });
        SetCell(ws, zeile, 2, cell =>
        {
            cell.Value = "Artikel";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Left);
        });
        SetCell(ws, zeile, 3, cell =>
        {
            cell.Value = "Gewicht";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Right);
        });
        SetCell(ws, zeile, 4, cell =>
        {
            cell.Value = "EAN";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Right);
        });
        SetCell(ws, zeile, 5, cell =>
        {
            cell.Value = "Bestand";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        SetCell(ws, zeile, 6, cell =>
        {
            cell.Value = "Soll";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        SetCell(ws, zeile, 7, cell =>
        {
            cell.Value = "Fehlt";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        
        foreach (var item in items)
        {
            zeile++;
            ws.Row(zeile).Height = 25;
            SetCell(ws, zeile, 1, cell =>
            {
                cell.Value = $"{item.ArticleNr}";
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Left);
            });
            SetCell(ws, zeile, 2, cell =>
            {
                cell.Value = $"{item.ArticleDisplayName}";
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Left);
            });
            SetCell(ws, zeile, 3, cell =>
            {
                cell.Value = item.UnitWeight;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Right);
            });
            SetCell(ws, zeile, 4, cell =>
            {
                cell.Value = $"{item.Ean}";
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Right);
            });
            SetCell(ws, zeile,5, cell =>
            {
                cell.Value = item.Count;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
            SetCell(ws, zeile, 6, cell =>
            {
                cell.Value = item.RequiredCount;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
            SetCell(ws, zeile, 7, cell =>
            {
                if (item.RequiredCount is not null && item.Count < item.RequiredCount)
                {
                    cell.Value = item.RequiredCount - item.Count;
                }
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
        }
    }
    private static void SetHeaderStyle(IXLCell cell)
    {
        cell.Style.Font.SetBold(true)
            .Font.SetFontSize(18)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    }
    private static void SetHeader2Style(IXLCell cell, XLAlignmentHorizontalValues horizonalAlignment)
    {
        cell.Style.Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(horizonalAlignment)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    }
    private static void SetTableHeaderStyle(IXLCell cell, XLAlignmentHorizontalValues horizonalAlignment)
    {
        cell.Style.Font.SetBold(true)
            .Font.SetFontSize(12)
            .Fill.SetBackgroundColor(XLColor.LightGray)            
            .Alignment.SetHorizontal(horizonalAlignment)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Bottom)
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            ;
    }
    private static void SetTableDataStyle(IXLCell cell, XLAlignmentHorizontalValues horizonalAlignment)
    {
        cell.Style.Font.SetBold(false)
            .Font.SetFontSize(12)
            .Alignment.SetHorizontal(horizonalAlignment)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            ;
    }

    private static void SetCell(IXLWorksheet ws, int row, int col, Action<IXLCell> cellSetup)
    {
        cellSetup(ws.Cell(row, col));
    }
} 