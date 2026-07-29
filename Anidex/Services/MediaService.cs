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
        return (await _malService.GetRecommendedAnimeAsync())
            .Where(media => !media.IsAdult)
            .ToList();
    }

    // Visual Novel recommendations have been disabled to prevent accidental exposure to adult content
    public async Task<List<Media>> GetRecommendedVisualNovelsAsync()
    {
        // Return empty list to disable VN recommendations
        return new List<Media>();
    }

    public async Task<List<Media>> SearchMediaAsync(string query)
    {
        var animeTask = _malService.SearchAnimeAsync(query);
        var vnTask = _vndbService.SearchVNAsync(query);

        var results = await Task.WhenAll(animeTask, vnTask);

        return results[0]
            .Concat(results[1])
            .Where(media => !media.IsAdult)
            .ToList();
    }

    public async Task<MediaDetails> GetMediaDetailsAsync(string id)
    {
        if (id.StartsWith("mal:"))
        {
            try
            {
                return await _malService.GetAnimeDetailsAsync(id);
            }
            catch (NotAllowedException)
            {
                throw;
            }
        }
        if (id.StartsWith("vndb:"))
        {
            try
            {
                return await _vndbService.GetVNDetailsAsync(id);
            }
            catch (NotAllowedException)
            {
                throw;
            }
        }

        throw new ArgumentException("Invalid media ID format. Must start with 'mal:' or 'vndb:'.");
    }
}
