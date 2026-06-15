using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Games;

public partial class LearningModulesViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly ProfileService _profileService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<Category> _categories = new();

    public LearningModulesViewModel(
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
        var profile = _profileService.GetActiveProfile();
        if (profile is null) return;
        var cats = await _contentService.GetCategoriesAsync(profile);
        foreach (var cat in cats)
        {
            if (profile.CategoryProgresses.TryGetValue(cat.Id, out var cp))
                cat.ProgressPercent = cp.BestStars * 33;
        }
        Categories = cats;
    }

    [RelayCommand]
    public Task OpenCategoryAsync(Category category) =>
        Shell.Current.GoToAsync($"category?categoryId={Uri.EscapeDataString(category.Id)}");

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
