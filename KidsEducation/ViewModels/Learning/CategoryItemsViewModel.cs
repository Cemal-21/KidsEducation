using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Learning;

[QueryProperty(nameof(CategoryId), "categoryId")]
public partial class CategoryItemsViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private List<LearningItem> _items = new();
    [ObservableProperty] private bool _isLoading = true;

    // ── Yeni: header için kategori bilgileri ─────────────────
    [ObservableProperty] private string _categoryColorHex = "#6C62F5";
    [ObservableProperty] private string _categoryColorHex2 = "#4C44C6";
    [ObservableProperty] private string _categoryEmoji = "📚";
    [ObservableProperty] private string _categoryImage = "ui_learning_3d.png";
    [ObservableProperty] private string _title = string.Empty;
    // ────────────────────────────────────────────────────────

    public CategoryItemsViewModel(ContentService contentService, NavigationService navigationService)
    {
        _contentService = contentService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            // Kategori bilgilerini yükle
            var category = await _contentService.GetCategoryAsync(CategoryId);
            if (category is not null)
            {
                Title = category.NameTr;
                CategoryColorHex = category.ColorHex;
                CategoryColorHex2 = category.ColorHex2;
                CategoryEmoji = category.Emoji;
                CategoryImage = string.IsNullOrWhiteSpace(category.Image)
                    ? "ui_learning_3d.png"
                    : category.Image;
            }
            else
            {
                Title = CategoryId;
            }

            Items = await _contentService.GetItemsAsync(CategoryId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectItemAsync(LearningItem item)
    {
        if (item is null) return;
        await _navigationService.GoToLearningItemAsync(CategoryId, item.Id);
    }

    [RelayCommand]
    public Task GoBackAsync() => _navigationService.GoBackAsync();
}
