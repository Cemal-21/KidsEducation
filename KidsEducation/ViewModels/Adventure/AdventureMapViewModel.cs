using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;
using System.Collections.ObjectModel;

namespace KidsEducation.ViewModels.Adventure;

public partial class AdventureMapViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private ChildProfile? _activeProfile;
    [ObservableProperty] private string _summaryText = "";

    public ObservableCollection<AdventureNodeCard> Nodes { get; } = new();

    public AdventureMapViewModel(
        ContentService contentService,
        ProfileService profileService,
        NavigationService navigationService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            ActiveProfile = _profileService.GetActiveProfile();
            if (ActiveProfile is null)
                return;

            var categories = await _contentService.GetCategoriesAsync(ActiveProfile);
            Nodes.Clear();

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                ActiveProfile.CategoryProgresses.TryGetValue(category.Id, out var progress);
                var stars = progress?.BestStars ?? 0;
                var playCount = progress?.PlayCount ?? 0;
                var module = ModuleProgressService.GetModuleForCategory(category.Id);

                Nodes.Add(new AdventureNodeCard
                {
                    StepNumber = i + 1,
                    CategoryId = category.Id,
                    Title = category.NameTr,
                    Emoji = category.Emoji,
                    ImagePath = category.Image,
                    ModuleName = module.Name,
                    ModuleEmoji = module.Emoji,
                    Stars = stars,
                    PlayCount = playCount,
                    IsUnlocked = i < 3 || playCount > 0 || ActiveProfile.TotalLessonsCompleted >= i
                });
            }

            var unlocked = Nodes.Count(n => n.IsUnlocked);
            var completed = Nodes.Count(n => n.Stars >= 3);
            SummaryText = $"{unlocked}/{Nodes.Count} durak açık • {completed} durak tamam";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public Task SelectNodeAsync(AdventureNodeCard node)
    {
        if (!node.IsUnlocked)
            return Task.CompletedTask;

        return _navigationService.GoToCategoryAsync(node.CategoryId);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}

public class AdventureNodeCard
{
    public int StepNumber { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleEmoji { get; set; } = string.Empty;
    public int Stars { get; set; }
    public int PlayCount { get; set; }
    public bool IsUnlocked { get; set; }

    public string StepText => StepNumber.ToString("00");
    public string StatusText => IsUnlocked
        ? Stars >= 3 ? "Tamamlandı" : PlayCount > 0 ? $"{Stars}/3 yıldız" : "Başla"
        : "Kilitli";
    public string StarsText => Stars <= 0 ? "☆☆☆" : new string('★', Stars) + new string('☆', Math.Max(0, 3 - Stars));
    public string BackgroundColor => IsUnlocked ? "#FFFFFF" : "#F1F5F9";
    public string BorderColor => IsUnlocked ? "#DCD6FF" : "#CBD5E1";
    public double ImageOpacity => IsUnlocked ? 1 : 0.35;
    public string StatusColor => IsUnlocked ? "#5148D4" : "#64748B";
}
