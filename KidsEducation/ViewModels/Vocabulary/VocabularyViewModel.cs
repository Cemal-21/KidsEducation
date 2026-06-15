using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Vocabulary;

public partial class VocabularyViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly AudioService _audioService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _selectedCategoryId = "all";
    [ObservableProperty] private string _totalCountText = "0 kelime";
    [ObservableProperty] private string _treasureSummaryText = "0 ödül açıldı";

    public ObservableCollection<VocabFilterChip> FilterChips { get; } = new();
    public ObservableCollection<VocabWordCard> DisplayedWords { get; } = new();
    public ObservableCollection<TreasureRewardCard> TreasureRewards { get; } = new();

    private List<VocabWordCard> _allWords = new();

    public VocabularyViewModel(
        ContentService contentService,
        ProfileService profileService,
        AudioService audioService,
        NavigationService navigationService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _audioService = audioService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var profile = _profileService.GetActiveProfile();
            if (profile is null) return;

            var allCategories = await _contentService.GetCategoriesAsync(profile);

            // Oynanmış kategorileri al (en az 1 kez oynanan)
            var playedCategories = allCategories
                .Where(c => profile.CategoryProgresses.TryGetValue(c.Id, out var cp) && cp.PlayCount >= 1)
                .ToList();

            // Filtre chip'leri
            FilterChips.Clear();
            FilterChips.Add(new VocabFilterChip { CategoryId = "all", Label = "Tümü", Emoji = "📚", IsSelected = true });
            foreach (var cat in playedCategories)
                FilterChips.Add(new VocabFilterChip { CategoryId = cat.Id, Label = cat.NameTr, Emoji = cat.Emoji });

            // Tüm kelimeler
            _allWords.Clear();
            foreach (var cat in playedCategories)
            {
                var items = await _contentService.GetItemsAsync(cat.Id);
                foreach (var item in items)
                {
                    _allWords.Add(new VocabWordCard
                    {
                        ItemId = item.Id,
                        CategoryId = cat.Id,
                        CategoryEmoji = cat.Emoji,
                        CategoryNameTr = cat.NameTr,
                        CategoryColor = cat.ColorHex,
                        NameTr = item.NameTr,
                        NameEn = item.NameEn,
                        ImagePath = item.ImagePath
                    });
                }
            }

            LoadTreasureRewards(profile);
            ApplyFilter("all");
            TotalCountText = $"{_allWords.Count} kelime • {TreasureRewards.Count(r => r.IsUnlocked)} ödül";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void SelectFilter(VocabFilterChip chip)
    {
        foreach (var c in FilterChips)
            c.IsSelected = c.CategoryId == chip.CategoryId;

        SelectedCategoryId = chip.CategoryId;
        ApplyFilter(chip.CategoryId);
    }

    [RelayCommand]
    public async Task PlayWordAsync(VocabWordCard card)
    {
        await _audioService.PlayItemSoundAsync(card.ItemId);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();

    private void ApplyFilter(string categoryId)
    {
        DisplayedWords.Clear();
        var filtered = categoryId == "all"
            ? _allWords
            : _allWords.Where(w => w.CategoryId == categoryId).ToList();

        foreach (var word in filtered)
            DisplayedWords.Add(word);
    }

    private void LoadTreasureRewards(ChildProfile profile)
    {
        TreasureRewards.Clear();

        var rewards = new[]
        {
            TreasureRewardCard.FromProgress("🎁", "İlk Sandık", "1 oyun bitir", profile.TotalLessonsCompleted, 1),
            TreasureRewardCard.FromProgress("⭐", "Yıldız Kesesi", "10 yıldız topla", profile.TotalStars, 10),
            TreasureRewardCard.FromProgress("🔥", "Seri Rozeti", "3 gün üst üste oyna", profile.StreakDays, 3),
            TreasureRewardCard.FromProgress("💎", "Bilgi Taşı", "250 XP kazan", profile.TotalXp, 250),
            TreasureRewardCard.FromProgress("👑", "Küçük Usta", "25 oyun bitir", profile.TotalLessonsCompleted, 25),
            TreasureRewardCard.FromProgress("🌟", "Parlak Koleksiyon", "50 yıldız topla", profile.TotalStars, 50),
        };

        foreach (var reward in rewards)
            TreasureRewards.Add(reward);

        TreasureSummaryText = $"{TreasureRewards.Count(r => r.IsUnlocked)}/{TreasureRewards.Count} ödül açıldı";
    }
}

public partial class VocabFilterChip : ObservableObject
{
    public string CategoryId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;

    [ObservableProperty] private bool _isSelected;

    public string BackgroundColor => IsSelected ? "#5148D4" : "#F0F4FF";
    public string TextColor => IsSelected ? "#FFFFFF" : "#5148D4";
    public string BorderColor => IsSelected ? "#5148D4" : "#D0CCFF";

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(BorderColor));
    }
}

public class VocabWordCard
{
    public string ItemId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryEmoji { get; set; } = string.Empty;
    public string CategoryNameTr { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#5148D4";
    public string NameTr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}

public class TreasureRewardCard
{
    public string Emoji { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Target { get; set; }
    public bool IsUnlocked => Progress >= Target;
    public double ProgressRatio => Target <= 0 ? 0 : Math.Min(1, (double)Progress / Target);
    public string ProgressText => IsUnlocked ? "Açıldı" : $"{Math.Min(Progress, Target)}/{Target}";
    public string BackgroundColor => IsUnlocked ? "#FFF8E1" : "#F8FAFC";
    public string BorderColor => IsUnlocked ? "#FBBF24" : "#E2E8F0";
    public string TextColor => IsUnlocked ? "#92400E" : "#64748B";

    public static TreasureRewardCard FromProgress(
        string emoji,
        string title,
        string description,
        int progress,
        int target) =>
        new()
        {
            Emoji = emoji,
            Title = title,
            Description = description,
            Progress = progress,
            Target = target
        };
}
