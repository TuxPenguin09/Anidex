namespace Anidex.Services;

using System.Text;
using System.Text.Json;
using Anidex.Models;
using Microsoft.Extensions.Logging;

public class VNDBService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VNDBService> _logger;

    public VNDBService(HttpClient httpClient, ILogger<VNDBService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<Media>> GetRecommendedVisualNovelsAsync()
    {
        try
        {
            // Kana does not expose an "updated" sort for VNs. Released is the closest
            // supported signal for a current recommendations feed.
            var requestBody = new
            {
                filters = new object[]
                {
                    "and",
                    new object[] { "released", "<=", DateTime.UtcNow.ToString("yyyy-MM-dd") },
                    new object[] { "has_description", "=", 1 }
                },
                fields = "title,description,released,image.url,image.thumbnail,rating,votecount",
                sort = "released",
                reverse = true,
                results = 20
            };

            var response = await PostApiRequestAsync("vn", requestBody);

            var media = new List<Media>();
            if (response.TryGetProperty("results", out var results))
            {
                foreach (var vn in results.EnumerateArray())
                {
                    media.Add(ParseVisualNovel(vn));
                }
            }

            return media;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to fetch recently released visual novels from VNDB");
            return new List<Media>();
        }
    }

    public async Task<List<Media>> SearchVNAsync(string query)
    {
        try
        {
            var requestBody = new
            {
                filters = new object[] { "search", "=", query },
                fields = "title,description,released,image.url,image.thumbnail,rating,votecount",
                sort = "searchrank",
                results = 10
            };

            var response = await PostApiRequestAsync("vn", requestBody);

            var media = new List<Media>();
            if (response.TryGetProperty("results", out var results))
            {
                foreach (var vn in results.EnumerateArray())
                {
                    media.Add(ParseVisualNovel(vn));
                }
            }

            return media;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to search VNDB for {Query}", query);
            return new List<Media>();
        }
    }

    public async Task<MediaDetails> GetVNDetailsAsync(string id)
    {
        try
        {
            // Extract VNID from format "vndb:v12345"
            var vnId = id.StartsWith("vndb:", StringComparison.OrdinalIgnoreCase)
                ? id["vndb:".Length..]
                : id;

            if (string.IsNullOrWhiteSpace(vnId))
            {
                throw new ArgumentException("A VNDB visual novel ID is required.", nameof(id));
            }

            var requestBody = new
            {
                filters = new object[] { "id", "=", vnId },
                fields = "title,alttitle,description,released,image.url,image.thumbnail,rating,average,votecount,length_minutes,languages,platforms,developers.name,tags.name,tags.rating,tags.spoiler"
            };

            var response = await PostApiRequestAsync("vn", requestBody);

            if (response.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var vn = results[0];
                var title = vn.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "Unknown" : "Unknown";
                var description = vn.TryGetProperty("description", out var descEl) && descEl.ValueKind != JsonValueKind.Null
                    ? descEl.GetString() ?? "No description available."
                    : "No description available.";

                var characters = await GetCharacterNamesAsync(vnId);

                return new MediaDetails
                {
                    Title = title,
                    Description = description,
                    Characters = characters,
                    CoverImage = GetImageUrl(vn),
                    AlternativeTitle = GetString(vn, "alttitle"),
                    Score = GetScore(vn, "rating"),
                    VoteCount = GetInt32(vn, "votecount"),
                    Released = GetString(vn, "released"),
                    LengthMinutes = GetInt32(vn, "length_minutes"),
                    Languages = GetStringValues(vn, "languages"),
                    Platforms = GetStringValues(vn, "platforms"),
                    Developers = GetObjectNames(vn, "developers"),
                    Tags = GetTagNames(vn),
                    Source = "VNDB",
                    ExternalUrl = $"https://vndb.org/{vnId}"
                };
            }

            throw new Exception($"VN with ID {vnId} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to fetch VNDB details for {VnId}", id);
            throw;
        }
    }

    private async Task<List<string>> GetCharacterNamesAsync(string vnId)
    {
        try
        {
            var requestBody = new
            {
                filters = new object[] { "vn", "=", new object[] { "id", "=", vnId } },
                fields = "name",
                sort = "name",
                results = 12
            };

            var response = await PostApiRequestAsync("character", requestBody);
            if (!response.TryGetProperty("results", out var results))
            {
                return new List<string>();
            }

            return results.EnumerateArray()
                .Select(character => GetString(character, "name"))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            // Character data is supplementary; a VN detail page remains useful without it.
            _logger.LogWarning(ex, "Unable to fetch characters for VNDB visual novel {VnId}", vnId);
            return new List<string>();
        }
    }

    private async Task<JsonElement> PostApiRequestAsync(string endpoint, object requestBody)
    {
        var jsonContent = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(endpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"VNDB returned {(int)response.StatusCode} ({response.ReasonPhrase}) for {endpoint}: {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static Media ParseVisualNovel(JsonElement vn)
    {
        var id = vn.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        var title = vn.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "Unknown" : "Unknown";
        var rating = GetScore(vn, "rating");

        return new Media
        {
            Id = $"vndb:{id}",
            Source = "VNDB",
            Title = title,
            Description = GetString(vn, "description"),
            CoverImage = GetImageUrl(vn),
            Score = rating,
            Released = GetString(vn, "released")
        };
    }

    private static string? GetImageUrl(JsonElement vn)
    {
        if (!vn.TryGetProperty("image", out var image) || image.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(image, "thumbnail") ?? GetString(image, "url");
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static double? GetScore(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetDouble() / 10.0
            : null;
    }

    private static List<string> GetStringValues(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();
    }

    private static List<string> GetObjectNames(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return values.EnumerateArray()
            .Select(value => GetString(value, "name"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetTagNames(JsonElement element)
    {
        if (!element.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return tags.EnumerateArray()
            .Where(tag => !tag.TryGetProperty("spoiler", out var spoiler) || spoiler.ValueKind != JsonValueKind.Number || spoiler.GetInt32() == 0)
            .Select(tag => GetString(tag, "name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }
}

public class MediaDetails
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Characters { get; set; } = new();
    public string? CoverImage { get; set; }
    public string? AlternativeTitle { get; set; }
    public double? Score { get; set; }
    public int? VoteCount { get; set; }
    public string? Released { get; set; }
    public int? LengthMinutes { get; set; }
    public List<string> Languages { get; set; } = new();
    public List<string> Platforms { get; set; } = new();
    public List<string> Developers { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? Source { get; set; }
    public string? ExternalUrl { get; set; }
}
