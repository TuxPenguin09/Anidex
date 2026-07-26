using System.Text.Json;
using Anidex.Models;

namespace Anidex.Services;

public class MalService
{
    private readonly HttpClient _httpClient;

    public MalService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Media>> GetRecommendedAnimeAsync()
    {
        // Implement logic to get recommended anime using _httpClient
        var response = await _httpClient.GetAsync("recommendations/anime");

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var media = new List<Media>();

        foreach (var item in document.RootElement.GetProperty("data").EnumerateArray())
        {
            var entry = item.GetProperty("entry")[0];

            media.Add(new Media
            {
                Id = $"mal:{entry.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = entry.GetProperty("title").GetString() ?? "",
                CoverImage = entry.GetProperty("images")
                    .GetProperty("jpg")
                    .GetProperty("image_url")
                    .GetString(),
            });
        }
        
        return media;
    }
}