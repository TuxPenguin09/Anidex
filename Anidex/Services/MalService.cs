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

    public async Task<List<Media>> GetSummerAnimeAsync(int year, string season)
    {
        if (string.IsNullOrWhiteSpace(season))
        {
            season = "summer";
        }

        var response = await _httpClient.GetAsync($"seasons/{year}/{season}?sfw=true&limit=25");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var media = new List<Media>();
        foreach (var entry in document.RootElement.GetProperty("data").EnumerateArray())
        {
            // Defence-in-depth: sfw=true already excludes adult titles, but re-check
            // so a future API change can never accidentally surface Rx content.
            if (IsAdultContent(entry))
            {
                continue;
            }

            media.Add(new Media
            {
                Id = $"mal:{entry.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = entry.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = GetCoverImageUrl(entry),
                Score = ReadScore(entry),
                Released = ReadReleased(entry),
                IsAdult = false,
            });
        }

        return media;
    }

    public async Task<List<Media>> GetCurrentSeasonAsync()
    {
        var response = await _httpClient.GetAsync("seasons/now?sfw=true&limit=25");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var media = new List<Media>();
        foreach (var entry in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (IsAdultContent(entry))
            {
                continue;
            }

            media.Add(new Media
            {
                Id = $"mal:{entry.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = entry.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = GetCoverImageUrl(entry),
                Score = ReadScore(entry),
                Released = ReadReleased(entry),
                IsAdult = false,
            });
        }

        return media;
    }

    public async Task<List<RankedMediaItem>> GetTopAiringAnimeAsync()
    {
        var response = await _httpClient.GetAsync("top/anime?filter=airing&limit=10&sfw=true");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var items = new List<RankedMediaItem>();
        var rank = 1;
        foreach (var entry in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (IsAdultContent(entry))
            {
                continue;
            }

            items.Add(BuildRankedAiring(entry, rank++));
            if (items.Count >= 10)
            {
                break;
            }
        }

        return items;
    }

    public async Task<List<RankedMediaItem>> GetMostPopularAnimeAsync()
    {
        var response = await _httpClient.GetAsync("top/anime?filter=bypopularity&limit=10&sfw=true");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var items = new List<RankedMediaItem>();
        var rank = 1;
        foreach (var entry in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (IsAdultContent(entry))
            {
                continue;
            }

            items.Add(BuildRankedPopular(entry, rank++));
            if (items.Count >= 10)
            {
                break;
            }
        }

        return items;
    }

    public async Task<List<Media>> GetRecentlyAddedAnimeAsync()
    {
        var response = await _httpClient.GetAsync("anime?order_by=start_date&sort=desc&limit=12&sfw=true");
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var media = new List<Media>();
        foreach (var entry in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (IsAdultContent(entry))
            {
                continue;
            }

            media.Add(new Media
            {
                Id = $"mal:{entry.GetProperty("mal_id").GetInt32()}",
                Source = "MAL",
                Title = entry.GetProperty("title").GetString() ?? string.Empty,
                CoverImage = GetCoverImageUrl(entry),
                Score = ReadScore(entry),
                Released = ReadReleased(entry),
                IsAdult = false,
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
            var response = await GetWithRetryAsync($"anime/{malId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"MAL API Error: {response.StatusCode} for anime/{malId}");
                var msg = $"Jikan returned {(int)response.StatusCode} {response.ReasonPhrase} for anime/{malId}";
                throw new MalApiException((int)response.StatusCode, msg);
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

    private async Task<HttpResponseMessage> GetWithRetryAsync(string relativeUrl)
    {
        // Retries only on transient upstream errors (5xx + 429). 4xx is treated
        // as a caller error (bad id, etc.) and surfaced immediately.
        const int maxAttempts = 2;
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            response = await _httpClient.GetAsync(relativeUrl);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var code = (int)response.StatusCode;
            var transient = code >= 500 || code == 429;
            if (!transient || attempt == maxAttempts)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(500);
        }

        return response!;
    }

    private static double? ReadScore(JsonElement entry)
        => entry.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number
            ? s.GetDouble()
            : (double?)null;

    private static string? ReadReleased(JsonElement entry)
    {
        if (entry.TryGetProperty("aired", out var aired) &&
            aired.ValueKind == JsonValueKind.Object &&
            aired.TryGetProperty("string", out var s) &&
            s.ValueKind == JsonValueKind.String)
        {
            return s.GetString();
        }
        return null;
    }

    private static RankedMediaItem BuildRankedAiring(JsonElement entry, int rank)
    {
        var title = entry.GetProperty("title").GetString() ?? string.Empty;
        var malId = entry.GetProperty("mal_id").GetInt32();
        var type = entry.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString() : null;
        var eps = entry.TryGetProperty("episodes", out var e) && e.ValueKind == JsonValueKind.Number
            ? (int?)e.GetInt32() : null;
        var score = ReadScore(entry);

        var subtitleParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) subtitleParts.Add(type!);
        if (eps.HasValue) subtitleParts.Add($"{eps} eps");
        if (score.HasValue) subtitleParts.Add($"scored {score.Value:0.00}");

        return new RankedMediaItem
        {
            Rank = rank,
            Title = title,
            CoverImage = GetCoverImageUrl(entry),
            Subtitle = string.Join(", ", subtitleParts),
            DetailsUrl = $"/media/mal:{malId}",
        };
    }

    private static RankedMediaItem BuildRankedPopular(JsonElement entry, int rank)
    {
        var title = entry.GetProperty("title").GetString() ?? string.Empty;
        var malId = entry.GetProperty("mal_id").GetInt32();
        var type = entry.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString() : null;
        var members = entry.TryGetProperty("members", out var m) && m.ValueKind == JsonValueKind.Number
            ? (int?)m.GetInt32() : null;

        var subtitleParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) subtitleParts.Add(type!);
        if (members.HasValue) subtitleParts.Add($"{members.Value:N0} members");

        return new RankedMediaItem
        {
            Rank = rank,
            Title = title,
            CoverImage = GetCoverImageUrl(entry),
            Subtitle = string.Join(", ", subtitleParts),
            DetailsUrl = $"/media/mal:{malId}",
        };
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
