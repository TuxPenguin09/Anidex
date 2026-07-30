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

    /// <summary>
    /// Summer 2026 anime. Tries <c>seasons/2026/summer</c> first; if Jikan hasn't
    /// populated that bucket yet, falls back to the current season so the section
    /// isn't empty in the meantime. Adult content is filtered defensively.
    /// </summary>
    public async Task<List<Media>> GetSummerAnimeAsync()
    {
        try
        {
            var result = await _malService.GetSummerAnimeAsync(2026, "summer");
            if (result.Count > 0)
            {
                return result.Where(m => !m.IsAdult).ToList();
            }

            var fallback = await _malService.GetCurrentSeasonAsync();
            return fallback.Where(m => !m.IsAdult).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MediaService.GetSummerAnimeAsync: {ex.Message}");
            return new List<Media>();
        }
    }

    public async Task<List<RankedMediaItem>> GetTopAiringAnimeAsync()
    {
        try
        {
            return await _malService.GetTopAiringAnimeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MediaService.GetTopAiringAnimeAsync: {ex.Message}");
            return new List<RankedMediaItem>();
        }
    }

    public async Task<List<RankedMediaItem>> GetMostPopularAnimeAsync()
    {
        try
        {
            return await _malService.GetMostPopularAnimeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MediaService.GetMostPopularAnimeAsync: {ex.Message}");
            return new List<RankedMediaItem>();
        }
    }

    public async Task<List<Media>> GetRecentlyAddedAnimeAsync()
    {
        try
        {
            return (await _malService.GetRecentlyAddedAnimeAsync())
                .Where(m => !m.IsAdult)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MediaService.GetRecentlyAddedAnimeAsync: {ex.Message}");
            return new List<Media>();
        }
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
