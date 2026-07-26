namespace Anidex.Services;

public class VNDBService
{
    private readonly HttpClient _httpClient;

    public VNDBService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}