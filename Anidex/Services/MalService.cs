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
        var response = await _httpClient.GetAsync("recommendations/anime");

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var media = new List<Media>();

        foreach (var item in document.RootElement.GetProperty("data").EnumerateArray())
        {
            var entry = item.GetProperty("entry")[0];
            var coverImage = GetCoverImageUrl(entry);

            media.Add(new Media
            {
                Id = $"mal:{entry.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = entry.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = coverImage,
            });
        }

        return media;
    }

    private static string? GetCoverImageUrl(JsonElement entry)
    {
        if (!entry.TryGetProperty("images", out var images))
        {
            return null;
        }

        if (TryGetImageUrl(images, "jpg", out var imageUrl) ||
            TryGetImageUrl(images, "webp", out imageUrl))
        {
            return imageUrl;
        }

        return null;
    }

    private static bool TryGetImageUrl(JsonElement images, string format, out string? imageUrl)
    {
        imageUrl = null;

        if (!images.TryGetProperty(format, out var formatNode))
        {
            return false;
        }

        if (formatNode.TryGetProperty("large_image_url", out var largeImage) && largeImage.ValueKind == JsonValueKind.String)
        {
            imageUrl = largeImage.GetString();

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                return true;
            }
        }

        if (formatNode.TryGetProperty("image_url", out var image) && image.ValueKind == JsonValueKind.String)
        {
            imageUrl = image.GetString();
            return !string.IsNullOrWhiteSpace(imageUrl);
        }

        return false;
    }
}