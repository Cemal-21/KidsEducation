using System.Text.Json;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class SongService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<SongItem>> GetSongsAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("songs.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("songs")
                .Deserialize<List<SongItem>>(JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SongService] songs.json yuklenemedi: {ex.Message}");
            return new List<SongItem>();
        }
    }

    public async Task<SongItem?> GetSongAsync(string songId)
    {
        var songs = await GetSongsAsync();
        return songs.FirstOrDefault(x => x.Id == songId);
    }
}
