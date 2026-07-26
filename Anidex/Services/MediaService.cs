using Anidex.Models;

namespace Anidex.Services;

public class MediaService
{
    private readonly MalService _malService;
    private readonly VNDBService _vndbService;

    public MediaService(MalService malService, VNDBService vndbService)
    {
        _malService = malService;
        _vndbService = vndbService;
    }

    public async Task<List<Media>> GetRecommendedAnimeAsync()
    {
        return await _malService.GetRecommendedAnimeAsync();
    }
}