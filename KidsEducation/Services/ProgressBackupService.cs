using System.Text.Json;

namespace KidsEducation.Services;

public class ProgressBackupService
{
    private readonly ProfileService _profileService;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ProgressBackupService(ProfileService profileService)
    {
        _profileService = profileService;
    }

    // ── Dışa Aktar ──────────────────────────────────────────
    public async Task<bool> ExportAsync()
    {
        try
        {
            var profiles = _profileService.GetAllProfiles();
            var backup = new BackupData
            {
                ExportedAt = DateTime.UtcNow,
                Version = 1,
                Profiles = profiles
            };

            var json = JsonSerializer.Serialize(backup, JsonOpts);
            var fileName = $"kidseducation_yedek_{DateTime.Now:yyyyMMdd_HHmm}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "İlerlemeyi Dışa Aktar",
                File  = new ShareFile(filePath, "application/json")
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── İçe Aktar ──────────────────────────────────────────
    public async Task<ImportResult> ImportAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Yedek dosyasını seç (.json)",
                FileTypes   = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json", "*/*" } },
                    { DevicePlatform.iOS,     new[] { "public.json" } },
                    { DevicePlatform.WinUI,   new[] { ".json" } },
                })
            });

            if (result is null)
                return ImportResult.Cancelled();

            var json = await File.ReadAllTextAsync(result.FullPath);
            var backup = JsonSerializer.Deserialize<BackupData>(json, JsonOpts);

            if (backup?.Profiles is not { Count: > 0 })
                return ImportResult.Failed("Dosyada geçerli profil verisi bulunamadı.");

            // Mevcut profilleri koru + yeni gelenleri birleştir
            var existing = _profileService.GetAllProfiles();
            var merged = 0;
            var added = 0;

            foreach (var imported in backup.Profiles)
            {
                var match = existing.FirstOrDefault(p => p.Id == imported.Id);
                if (match is not null)
                {
                    // Daha yeni veriyi kabul et
                    if (imported.LastPlayedAt > match.LastPlayedAt)
                    {
                        _profileService.SaveProfile(imported);
                        merged++;
                    }
                }
                else
                {
                    _profileService.SaveProfile(imported);
                    added++;
                }
            }

            return ImportResult.Success(added, merged);
        }
        catch (Exception ex)
        {
            return ImportResult.Failed(ex.Message);
        }
    }

    // ── Lokal yedek listesi ─────────────────────────────────
    public List<LocalBackupInfo> GetLocalBackups()
    {
        var dir = FileSystem.CacheDirectory;
        return Directory.GetFiles(dir, "kidseducation_yedek_*.json")
            .Select(p => new LocalBackupInfo
            {
                FilePath  = p,
                FileName  = Path.GetFileName(p),
                CreatedAt = File.GetCreationTime(p),
                SizeKb    = (int)(new FileInfo(p).Length / 1024.0 + 0.5)
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }
}

public class BackupData
{
    public DateTime ExportedAt { get; set; }
    public int Version { get; set; } = 1;
    public List<Models.ChildProfile> Profiles { get; set; } = new();
}

public class LocalBackupInfo
{
    public string FilePath  { get; set; } = "";
    public string FileName  { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int SizeKb { get; set; }
    public string DisplayName => $"{CreatedAt:dd MMM yyyy HH:mm} ({SizeKb} KB)";
}

public class ImportResult
{
    public bool IsSuccess   { get; private set; }
    public bool IsCancelled { get; private set; }
    public int AddedCount   { get; private set; }
    public int MergedCount  { get; private set; }
    public string? ErrorMessage { get; private set; }

    public string SummaryText => IsSuccess
        ? $"✅ {AddedCount} yeni profil eklendi, {MergedCount} profil güncellendi."
        : IsCancelled ? "" : $"❌ {ErrorMessage}";

    public static ImportResult Success(int added, int merged) =>
        new() { IsSuccess = true, AddedCount = added, MergedCount = merged };

    public static ImportResult Failed(string msg) =>
        new() { IsSuccess = false, ErrorMessage = msg };

    public static ImportResult Cancelled() =>
        new() { IsCancelled = true };
}
