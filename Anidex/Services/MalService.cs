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

    public async Task<List<Media>> SearchAnimeAsync(string query)
    {
        var response = await _httpClient.GetAsync($"anime?q={Uri.EscapeDataString(query)}");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var media = new List<Media>();
        foreach (var item in document.RootElement.GetProperty("data").EnumerateArray())
        {
            var coverImage = GetCoverImageUrl(item);
            media.Add(new Media
            {
                Id = $"mal:{item.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = item.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = coverImage,
            });
        }
        return media;
    }

    public async Task<MediaDetails> GetAnimeDetailsAsync(string id)
    {
        // Extract MAL ID from format "mal:12345"
        var malId = id.Replace("mal:", string.Empty);

        try
        {
            var response = await _httpClient.GetAsync($"anime/{malId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"MAL API Error: {response.StatusCode} for anime/{malId}");
                throw new HttpRequestException($"MAL API returned {response.StatusCode}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var data = document.RootElement.GetProperty("data");

            var title = data.GetProperty("title").GetString() ?? "Unknown";
            var synopsis = data.TryGetProperty("synopsis", out var synopsisElement) && synopsisElement.ValueKind != JsonValueKind.Null
                ? synopsisElement.GetString() ?? "No description available."
                : "No description available.";

            // Fetch characters from separate endpoint
            var characters = new List<string>();
            try
            {
                var charResponse = await _httpClient.GetAsync($"anime/{malId}/characters");
                if (charResponse.IsSuccessStatusCode)
                {
                    using var charStream = await charResponse.Content.ReadAsStreamAsync();
                    using var charDoc = await JsonDocument.ParseAsync(charStream);
                    var charData = charDoc.RootElement.GetProperty("data");

                    foreach (var c in charData.EnumerateArray().Take(10))
                    {
                        if (c.TryGetProperty("character", out var charInfo) &&
                            charInfo.TryGetProperty("name", out var charName) &&
                            charName.ValueKind == JsonValueKind.String)
                        {
                            characters.Add(charName.GetString() ?? "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching characters: {ex.Message}");
            }

            var coverImage = GetCoverImageUrl(data);

            return new MediaDetails
            {
                Title = title,
                Description = synopsis,
                Characters = characters,
                CoverImage = coverImage
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching anime details: {ex.Message}");
            throw; // Re-throw so the UI can handle it
        }
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
