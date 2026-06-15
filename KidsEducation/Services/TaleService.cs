using System.Text.Json;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class TaleService
{
    private List<Tale>? _cache;

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<List<Tale>> GetAllTalesAsync()
    {
        if (_cache is not null) return _cache;

        using var stream = await FileSystem.OpenAppPackageFileAsync("tales.json");
        _cache = await JsonSerializer.DeserializeAsync<List<Tale>>(stream, Opts) ?? new();
        return _cache;
    }

    public async Task<Tale?> GetTaleByIdAsync(string id)
    {
        var all = await GetAllTalesAsync();
        return all.FirstOrDefault(t => t.Id == id);
    }
}
