using System.Text.Json;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class LearningEventService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private string GetFilePath(string profileId) =>
        Path.Combine(FileSystem.AppDataDirectory, $"events_{profileId}.jsonl");

    public async Task LogEventAsync(LearningEvent evt)
    {
        try
        {
            var line = JsonSerializer.Serialize(evt, JsonOptions);
            await File.AppendAllTextAsync(GetFilePath(evt.ProfileId), line + Environment.NewLine);
        }
        catch
        {
            // analytics path — sessizce yut, oyun akışını bozma
        }
    }

    public async Task<List<LearningEvent>> GetRecentEventsAsync(string profileId, string categoryId, int take = 10)
    {
        var path = GetFilePath(profileId);
        if (!File.Exists(path)) return new();

        try
        {
            var lines = await File.ReadAllLinesAsync(path);
            return lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => JsonSerializer.Deserialize<LearningEvent>(l, JsonOptions))
                .Where(e => e is not null && e.CategoryId == categoryId)
                .Cast<LearningEvent>()
                .OrderByDescending(e => e.Timestamp)
                .Take(take)
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>Kategori bazlı son 7 gün doğruluk oranı — Parental rapor için</summary>
    public async Task<Dictionary<string, double>> GetWeeklyAccuracyByCategoryAsync(string profileId)
    {
        var path = GetFilePath(profileId);
        if (!File.Exists(path)) return new();

        var weekAgo = DateTime.UtcNow.AddDays(-7);

        try
        {
            var lines = await File.ReadAllLinesAsync(path);
            var events = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => JsonSerializer.Deserialize<LearningEvent>(l, JsonOptions))
                .Where(e => e is not null && e.Timestamp >= weekAgo)
                .Cast<LearningEvent>()
                .ToList();

            return events
                .GroupBy(e => e.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count(e => e.IsCorrect) / (double)g.Count());
        }
        catch
        {
            return new();
        }
    }
}