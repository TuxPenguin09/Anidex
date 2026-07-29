using Microsoft.AspNetCore.Mvc;
using Anidex.Services;

namespace Anidex.Controllers;

[ApiController]
[Route("visual-novels")]
public class VisualNovelsController : ControllerBase
{
    private readonly MediaService _mediaService;

    public VisualNovelsController(MediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchVisualNovels([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query parameter 'q' is required.");
        }

        var result = await _mediaService.SearchMediaAsync(q);

        // Filter to only VNDB results
        var vnResults = result.Where(m => m.Source == "VNDB").ToList();

        return Ok(vnResults);
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> GetVNDetails(string id)
    {
        if (!id.StartsWith("vndb:"))
        {
            id = $"vndb:{id}";
        }

        try
        {
            var result = await _mediaService.GetMediaDetailsAsync(id);
            return Ok(result);
        }
        catch (NotAllowedException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}