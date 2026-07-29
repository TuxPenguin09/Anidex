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
            
            // Check rating - skip Rx (Hentai/Adult) content
            if (IsAdultContent(entry))
            {
                continue;
            }

            var coverImage = GetCoverImageUrl(entry);

            media.Add(new Media
            {
                Id = $"mal:{entry.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = entry.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = coverImage,
                IsAdult = false
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
            // Check rating - skip Rx (Hentai/Adult) content
            if (IsAdultContent(item))
            {
                continue;
            }

            var coverImage = GetCoverImageUrl(item);
            media.Add(new Media
            {
                Id = $"mal:{item.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = item.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = coverImage,
                IsAdult = false
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

            // Check rating for adult content
            if (IsAdultContent(data))
            {
                throw new NotAllowedException("Not allowed to view this content. Adult content (Rx/Hentai) is restricted.");
            }

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
                CoverImage = coverImage,
                IsAdult = false
            };
        }
        catch (NotAllowedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching anime details: {ex.Message}");
            throw; // Re-throw so the UI can handle it
        }
    }

    private static bool IsAdultContent(JsonElement entry)
    {
        // Check the rating field for Rx (Hentai/Adult content)
        // MAL ratings: G, PG, PG-13, R (17+), R+ (Mild Nudity), Rx (Hentai/Adult)
        if (entry.TryGetProperty("rating", out var ratingElement) && ratingElement.ValueKind == JsonValueKind.String)
        {
            var rating = ratingElement.GetString() ?? string.Empty;
            // Rx is the hentai/adult rating
            if (rating.Equals("Rx", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check explicit_genres for Hentai content (more reliable according to API docs)
        // Hentai has mal_id = 12 in the genres list
        if (entry.TryGetProperty("explicit_genres", out var explicitGenresElement) &&
            explicitGenresElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var genre in explicitGenresElement.EnumerateArray())
            {
                if (genre.TryGetProperty("mal_id", out var malIdElement) &&
                    malIdElement.ValueKind == JsonValueKind.Number &&
                    malIdElement.GetInt32() == 12) // Hentai genre ID
                {
                    return true;
                }
            }
        }

        return false;
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
