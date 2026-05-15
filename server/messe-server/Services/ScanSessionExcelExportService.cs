using ClosedXML.Excel;
using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Dto;

namespace Herrmann.MesseApp.Server.Services;

public class ScanSessionExcelExportService(ILogger<ScanSessionExcelExportService> logger)
{
    public void Generate(Stream stream, string? dispatchSheetName, IEnumerable<DtoScanSessionArticle> scanSessionArticles, string workbookName, bool showExpectation)
    {
        logger.LogDebug("Excel Export started: {WorkbookName}", workbookName);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(workbookName);
        GenerateExcel(dispatchSheetName, scanSessionArticles.OrderBy(i => i.ArticleNr).ThenBy(i => i.UnitWeight), ws, showExpectation);
        workbook.SaveAs(stream);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    private static void GenerateExcel(string? dispatchSheetName, IEnumerable<DtoScanSessionArticle> items, IXLWorksheet ws, bool showExpectation)
    {
        var maxCol = showExpectation ? 7 : 5;
        ws.Column(1).Width = 10;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 20;
        if (showExpectation)
        {
            ws.Column(5).Width = 10;
            ws.Column(6).Width = 10;
            ws.Column(7).Width = 10;
        }
        else
        {
            ws.Column(5).Width = 20;
        }
        
        var zeile = 1;
        ws.Row(zeile).Height = 30;
        ws.Range(zeile, 1, zeile, maxCol).Merge();
        ws.Cell(zeile, 1).Value = showExpectation ? "Beladung" : "Bestandsaufnahme";
        SetHeaderStyle(ws.Cell(zeile, 1));

        zeile++;
        ws.Row(zeile).Height = 25;
        ws.Range(zeile, 1, zeile, 4).Merge();
        SetCell(ws, zeile, 1, cell =>
        {
            cell.Value = dispatchSheetName;
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
        if (showExpectation)
        {
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
        }
        
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
            if (showExpectation)
            {
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
    }
    public void GenerateCombined(Stream stream, ScanSession standSession, ScanSession lagerSession, IEnumerable<DtoCombinedArticle> articles)
    {
        logger.LogDebug("Combined Excel Export started: Stand={StandId}, Lager={LagerId}", standSession.Id, lagerSession.Id);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Kombinierte Übersicht");
        GenerateCombinedExcel(standSession, lagerSession, articles.OrderBy(a => a.ArticleNr).ThenBy(a => a.UnitWeight), ws);
        workbook.SaveAs(stream);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    private static void GenerateCombinedExcel(ScanSession standSession, ScanSession lagerSession, IEnumerable<DtoCombinedArticle> items, IXLWorksheet ws)
    {
        const int maxCol = 9;
        ws.Column(1).Width = 10;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 22;
        ws.Column(6).Width = 22;
        ws.Column(7).Width = 10;
        ws.Column(8).Width = 10;
        ws.Column(9).Width = 10;

        var standHeader = $"Stand — {standSession.StartedAt:dd.MM.yyyy HH:mm}";
        var lagerHeader = $"Lager — {lagerSession.StartedAt:dd.MM.yyyy HH:mm}";

        var zeile = 1;
        ws.Row(zeile).Height = 30;
        ws.Range(zeile, 1, zeile, maxCol).Merge();
        ws.Cell(zeile, 1).Value = "Kombinierte Übersicht";
        SetHeaderStyle(ws.Cell(zeile, 1));

        zeile++;
        ws.Row(zeile).Height = 25;
        ws.Range(zeile, 1, zeile, 4).Merge();
        SetCell(ws, zeile, 1, cell =>
        {
            cell.Value = $"Stand: {standSession.StartedAt:dd.MM.yyyy} / Lager: {lagerSession.StartedAt:dd.MM.yyyy}";
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
            cell.Value = standHeader;
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        SetCell(ws, zeile, 6, cell =>
        {
            cell.Value = lagerHeader;
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        SetCell(ws, zeile, 7, cell =>
        {
            cell.Value = "Gesamt";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        SetCell(ws, zeile, 8, cell =>
        {
            cell.Value = "Soll";
            SetTableHeaderStyle(cell, XLAlignmentHorizontalValues.Center);
        });
        SetCell(ws, zeile, 9, cell =>
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
            SetCell(ws, zeile, 5, cell =>
            {
                cell.Value = item.CountStand;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
            SetCell(ws, zeile, 6, cell =>
            {
                cell.Value = item.CountAnhaenger;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
            SetCell(ws, zeile, 7, cell =>
            {
                cell.Value = item.Total;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
            SetCell(ws, zeile, 8, cell =>
            {
                if (item.RequiredCount.HasValue)
                    cell.Value = item.RequiredCount.Value;
                SetTableDataStyle(cell, XLAlignmentHorizontalValues.Center);
            });
            SetCell(ws, zeile, 9, cell =>
            {
                if (item.Fehlt.HasValue)
                    cell.Value = item.Fehlt.Value;
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