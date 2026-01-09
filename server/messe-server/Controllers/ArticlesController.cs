using Herrmann.MesseApp.Server.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    [HttpGet(Name = "GetArticles")]
    public IEnumerable<DtoArticle> Get()
    {
        return [
            new DtoArticle{ Id = 1, Name = "Test", ArNr = "1234567890"},
            new DtoArticle{ Id = 2, Name = "Test2", ArNr = "1234567891"},
            new DtoArticle{ Id = 3, Name = "Test3", ArNr = "1234567892"}
        ];
    }
}
