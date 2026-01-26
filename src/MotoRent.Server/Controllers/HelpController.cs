using Microsoft.AspNetCore.Mvc;
using MotoRent.Services;
using System.Threading.Tasks;

namespace MotoRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpController : ControllerBase
{
    private readonly DocumentationSearchService m_searchService;

    public HelpController(DocumentationSearchService searchService)
    {
        m_searchService = searchService;
    }

    [HttpGet("ask")]
    public async Task<IActionResult> Ask([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Question cannot be empty.");
        }

        var answer = await m_searchService.AskGeminiAsync(q);
        return Ok(new { answer });
    }
}
