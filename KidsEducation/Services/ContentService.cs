using System.Text.Json;
using KidsEducation.Models;

namespace KidsEducation.Services;

public class ContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Kategoriler ───────────────────────────────────────────

    public async Task<List<Category>> GetCategoriesAsync(ChildProfile profile)
    {
        var all = await LoadAllCategoriesAsync();
        return all.Where(c => c.IsAvailableFor(profile)).ToList();
    }

    public async Task<Category?> GetCategoryAsync(string categoryId)
    {
        var all = await LoadAllCategoriesAsync();
        return all.FirstOrDefault(c => c.Id == categoryId);
    }

    // ── Son ders (Devam Et kartı) ─────────────────────────────

    public async Task<LastLessonInfo?> GetLastLessonAsync(ChildProfile profile)
    {
        try
        {
            // progress.json: profil bazlı ilerleme kaydı
            // Örnek yapı: { "lastLesson": { "lessonId": "...", ... } }
            using var stream = await FileSystem.OpenAppPackageFileAsync($"progress_{profile.Id}.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("lastLesson", out var lastLessonEl))
                return null;

            return lastLessonEl.Deserialize<LastLessonInfo>(JsonOptions);
        }
        catch
        {
            // Kayıt yoksa (ilk kullanım vb.) kart gösterilmez
            return null;
        }
    }

    // ── Günlük görev özeti ────────────────────────────────────

    public async Task<DailyGoalInfo> GetDailyGoalAsync(ChildProfile profile)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync($"progress_{profile.Id}.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("dailyGoal", out var dailyEl))
                return new DailyGoalInfo();

            return dailyEl.Deserialize<DailyGoalInfo>(JsonOptions) ?? new DailyGoalInfo();
        }
        catch
        {
            return new DailyGoalInfo();
        }
    }

    // ── Öğrenme öğeleri ───────────────────────────────────────

    public async Task<List<LearningItem>> GetItemsAsync(string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            return new List<LearningItem>();

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync($"{categoryId}.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(json);

            var items = doc.RootElement
                .GetProperty("items")
                .Deserialize<List<LearningItem>>(JsonOptions) ?? new();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Category))
                    item.Category = categoryId;
            }

            return items;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentService] {categoryId}.json yüklenemedi: {ex.Message}");
            return new List<LearningItem>();
        }
    }

    public async Task<LearningItem?> GetItemAsync(string categoryId, string itemId)
    {
        var items = await GetItemsAsync(categoryId);
        return items.FirstOrDefault(x => x.Id == itemId);
    }

    public async Task<List<LearningItem>> GetGameItemsAsync(string categoryId, int count = 8, int difficultyLevel = 0)
    {
        var all = await GetItemsAsync(categoryId);
        return PickGameItems(all, count, difficultyLevel);
    }

    /// <summary>
    /// Hafıza oyunu için kategori bağımsız, karışık içerikten rastgele item'lar döner.
    /// </summary>
    public async Task<List<LearningItem>> GetMixedGameItemsAsync(ChildProfile profile, int count = 6, int difficultyLevel = 0)
    {
        var categories = await GetCategoriesAsync(profile);
        var allItems = new List<LearningItem>();

        foreach (var category in categories)
        {
            var items = await GetItemsAsync(category.Id);
            allItems.AddRange(items);
        }

        return PickGameItems(allItems, count, difficultyLevel);
    }

    private static List<LearningItem> PickGameItems(List<LearningItem> items, int count, int difficultyLevel)
    {
        if (items.Count == 0)
            return new();

        if (difficultyLevel <= 0)
            return items.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();

        var preferred = items
            .Where(i => i.DifficultyLevel <= difficultyLevel)
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        var fallback = items
            .Where(i => i.DifficultyLevel > difficultyLevel)
            .OrderBy(i => i.DifficultyLevel)
            .ThenBy(_ => Guid.NewGuid())
            .ToList();

        return preferred
            .Concat(fallback)
            .Take(count)
            .OrderBy(_ => Guid.NewGuid())
            .ToList();
    }
    // ── Yardımcı ─────────────────────────────────────────────

    private async Task<List<Category>> LoadAllCategoriesAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("categories.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("categories")
                .Deserialize<List<Category>>(JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentService] categories.json yüklenemedi: {ex.Message}");
            return new List<Category>();
        }
    }
}
