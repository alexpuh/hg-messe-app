using Herrmann.MesseApp.Server.Dto;

namespace Herrmann.MesseApp.Server.Services;

public class ArticlesService
{
    private readonly Dictionary<int, DtoArticleUnit> articleUnits = new () {
        [1] = new DtoArticleUnit { UnitId = 1, ArticleNr = "1111", ArticleName = Guid.NewGuid().ToString("N"), EanBox = "40000001", EanUnit = "20000001", Gewicht = 100, UnitsPerBox = 3},
        [2] = new DtoArticleUnit { UnitId = 2, ArticleNr = "1111", ArticleName = Guid.NewGuid().ToString("N"), EanBox = "40000002", EanUnit = "20000002", Gewicht = 200, UnitsPerBox = 3},
        [3] = new DtoArticleUnit { UnitId = 3, ArticleNr = "3333", ArticleName = Guid.NewGuid().ToString("N"), EanBox = "40000003", EanUnit = "20000003", Gewicht = 100, UnitsPerBox = 3},
        [4] = new DtoArticleUnit { UnitId = 4, ArticleNr = "3333", ArticleName = Guid.NewGuid().ToString("N"), EanBox = "40000004", EanUnit = "20000004", Gewicht = 200, UnitsPerBox = 3},
        [5] = new DtoArticleUnit { UnitId = 5, ArticleNr = "4444", ArticleName = "Akropolis", EanBox = "40000004", EanUnit = "4260011993982", Gewicht = 200, UnitsPerBox = 3}
    };

    
    public bool TryGetArticleUnit(int unitId, out DtoArticleUnit? articleUnit)
    {
        return articleUnits.TryGetValue(unitId, out articleUnit!);
    }

    public bool TryFindEan(string ean, out DtoArticleUnit? articleUnit)
    {
        var found = articleUnits.Values.FirstOrDefault(x => x.EanBox == ean || x.EanUnit == ean);
        articleUnit = found;
        return found != null;
    }
}