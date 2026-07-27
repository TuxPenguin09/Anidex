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

    public async Task<List<Media>> GetRecommendedVisualNovelsAsync()
    {
        return await _vndbService.GetRecommendedVisualNovelsAsync();
    }

    public async Task<List<Media>> SearchMediaAsync(string query)
    {
        var animeTask = _malService.SearchAnimeAsync(query);
        var vnTask = _vndbService.SearchVNAsync(query);

        var results = await Task.WhenAll(animeTask, vnTask);

        return results[0].Concat(results[1]).ToList();
    }

    public async Task<MediaDetails> GetMediaDetailsAsync(string id)
    {
        if (id.StartsWith("mal:"))
        {
            return await _malService.GetAnimeDetailsAsync(id);
        }
        if (id.StartsWith("vndb:"))
        {
            return await _vndbService.GetVNDetailsAsync(id);
        }

        throw new ArgumentException("Invalid media ID format. Must start with 'mal:' or 'vndb:'.");
    }
}
