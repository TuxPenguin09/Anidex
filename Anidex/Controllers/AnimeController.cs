using Microsoft.AspNetCore.Mvc;
using Anidex.Services;

namespace Anidex.Controllers;

[ApiController]
[Route("anime")]
public class AnimeController : ControllerBase
{
    private readonly MediaService _mediaService;

    public AnimeController(MediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet("recommended")]
    public async Task<IActionResult> GetRecommendedAnime()
    {
        // Implement logic to get recommended anime using _mediaService
        var result = await _mediaService.GetRecommendedAnimeAsync();

        return Ok(result);
    }
}