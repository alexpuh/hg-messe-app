using System.Text.Json;
using Herrmann.MesseApp.Server.Data;
using Herrmann.MesseApp.Server.Data.Import;
using Herrmann.MesseApp.Server.Dto;
using Microsoft.EntityFrameworkCore;

namespace Herrmann.MesseApp.Server.Services;

public class ArticlesService
{
    private readonly MesseAppDbContext _dbContext;
    private readonly ILogger<ArticlesService> _logger;

    public ArticlesService(MesseAppDbContext dbContext, ILogger<ArticlesService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public bool TryGetArticleUnit(int unitId, out DtoArticleUnit? articleUnit)
    {
        var entity = _dbContext.ArticleUnits
            .AsNoTracking()
            .FirstOrDefault(x => x.UnitId == unitId);

        if (entity == null)
        {
            articleUnit = null;
            return false;
        }

        articleUnit = MapToDto(entity);
        return true;
    }

    public bool TryFindEan(string ean, out DtoArticleUnit? articleUnit)
    {
        var entity = _dbContext.ArticleUnits
            .AsNoTracking()
            .FirstOrDefault(x => x.EanBox == ean || x.EanUnit == ean);

        if (entity == null)
        {
            articleUnit = null;
            return false;
        }

        articleUnit = MapToDto(entity);
        return true;
    }

    public async Task<int> ImportFromJsonFileAsync(string filePath)
    {
        _logger.LogInformation("Starte Import von Artikeln aus Datei: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("JSON-Datei nicht gefunden", filePath);
        }

        var jsonContent = await File.ReadAllTextAsync(filePath);
        var articles = JsonSerializer.Deserialize<List<ArticleImport>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (articles == null || articles.Count == 0)
        {
            _logger.LogWarning("Keine Artikel in der JSON-Datei gefunden");
            return 0;
        }

        // Lösche alle vorhandenen Einträge
        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM ArticleUnits");

        var entities = articles
            .SelectMany(a => a.Units.Select(au => (article: a, unit: au)))
            .Select(paramAu =>
            {
                var (article, unit) = paramAu;
                return new ArticleUnit
                {
                    UnitId = unit.Id,
                    ArticleId = unit.ArticleId,
                    ArtNr = article.ArtNr,
                    Weight = unit.Weight,
                    DisplayName = article.DisplayName,
                    IsArticleDisabled = article.IsDisabled,
                    IsUnitDisabled = unit.IsDisabled,
                    PackagesInBox = unit.PackagesInBox,
                    EanUnit = string.IsNullOrWhiteSpace(unit.Ean) ? null : unit.Ean,
                    EanBox = null // Im Import-JSON scheint es nur Unit-EAN zu geben
                };
            }).ToList();
        
        await _dbContext.ArticleUnits.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("{Count} Artikel-Units erfolgreich importiert", entities.Count);
        return entities.Count;
    }

    public List<EanUnit> GetAllEanUnits()
    {
        var result = new List<EanUnit>();
        
        var units = _dbContext.ArticleUnits
            .AsNoTracking()
            .Where(x => !x.IsArticleDisabled && !x.IsUnitDisabled)
            .ToList();

        foreach (var unit in units)
        {
            if (!string.IsNullOrWhiteSpace(unit.EanUnit))
            {
                result.Add(new EanUnit(unit.UnitId, unit.EanUnit));
            }
            
            if (!string.IsNullOrWhiteSpace(unit.EanBox))
            {
                result.Add(new EanUnit(unit.UnitId, unit.EanBox));
            }
        }

        return result;
    }

    private static DtoArticleUnit MapToDto(ArticleUnit entity)
    {
        return new DtoArticleUnit
        {
            UnitId = entity.UnitId,
            ArticleNr = entity.ArtNr,
            ArticleName = entity.DisplayName,
            Gewicht = entity.Weight,
            EanUnit = entity.EanUnit,
            EanBox = entity.EanBox,
            UnitsPerBox = entity.PackagesInBox
        };
    }
    
    
}

public record EanUnit(int unitId, string ean);