using System.Net.Http.Headers;
using System.Text.Json;


namespace Infrastructure;

public class UpstashRedisClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public UpstashRedisClient(string url, string token)
    {
        _baseUrl = url;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<JsonElement> ExecuteAsync(params string[] args)
    {
        var url = $"{_baseUrl}/{string.Join("/", args.Select(Uri.EscapeDataString))}";
        var response = await _httpClient.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("result");
    }

    public async Task SetAsync(string key, string value)
        => await ExecuteAsync("SET", key, value);

    public async Task<string?> GetAsync(string key)
    {
        var result = await ExecuteAsync("GET", key);
        return result.ValueKind == JsonValueKind.Null ? null : result.GetString();
    }

    public async Task DeleteAsync(string key)
        => await ExecuteAsync("DEL", key);

    public async Task SAddAsync(string key, string value)
        => await ExecuteAsync("SADD", key, value);

    public async Task SRemAsync(string key, string value)
        => await ExecuteAsync("SREM", key, value);

    public async Task<string[]> SMembersAsync(string key)
    {
        var result = await ExecuteAsync("SMEMBERS", key);
        if (result.ValueKind == JsonValueKind.Null) return [];
        return result.EnumerateArray()
                     .Select(x => x.GetString()!)
                     .ToArray();
    }

    public async Task<long> SCardAsync(string key)
    {
        var result = await ExecuteAsync("SCARD", key);
        return result.GetInt64();
    }
}