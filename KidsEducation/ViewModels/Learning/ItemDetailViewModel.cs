using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsEducation.Models;
using KidsEducation.Services;

namespace KidsEducation.ViewModels.Learning;

[QueryProperty(nameof(CategoryId), "categoryId")]
[QueryProperty(nameof(ItemId), "itemId")]
public partial class ItemDetailViewModel : ObservableObject
{
    private readonly ContentService _contentService;
    private readonly NavigationService _navigationService;
    private readonly AudioService _audioService; // Servisimizi ekledik

    [ObservableProperty] private string _categoryId = string.Empty;
    [ObservableProperty] private string _itemId = string.Empty;
    [ObservableProperty] private LearningItem? _item;
    [ObservableProperty] private string _categoryColorHex = "#6C62F5";
    [ObservableProperty] private string _categoryColorHex2 = "#4C44C6";
    [ObservableProperty] private string _categoryEmoji = "📚";

    // Constructor'a AudioService'i dahil ettik
    public ItemDetailViewModel(ContentService contentService,
                               NavigationService navigationService,
                               AudioService audioService)
    {
        _contentService = contentService;
        _navigationService = navigationService;
        _audioService = audioService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(CategoryId) || string.IsNullOrWhiteSpace(ItemId))
            return;

        var category = await _contentService.GetCategoryAsync(CategoryId);
        if (category is not null)
        {
            CategoryColorHex = category.ColorHex;
            CategoryColorHex2 = category.ColorHex2;
            CategoryEmoji = category.Emoji;
        }

        Item = await _contentService.GetItemAsync(CategoryId, ItemId);
    }

    // ARTIK KENDİ SESLERİMİZİ KULLANIYORUZ
    [RelayCommand]
    public async Task SpeakAsync()
    {
        if (Item is null || string.IsNullOrWhiteSpace(Item.Id))
            return;

        // AudioService üzerinden, itemId ile eşleşen .mp3 dosyasını çalar
        await _audioService.PlayItemSoundAsync(Item.Id);
    }

    [RelayCommand]
    public async Task SpeakDescriptionAsync()
    {
        if (Item is null || string.IsNullOrWhiteSpace(Item.Id))
            return;

        await _audioService.SpeakDescriptionAsync(Item.Id);
    }

    [RelayCommand]
    public async Task SpeakFunFactAsync()
    {
        if (Item is null || string.IsNullOrWhiteSpace(Item.Id))
            return;

        await _audioService.SpeakFunFactAsync(Item.Id);
    }

    [RelayCommand]
    public Task GoBackAsync()
    {
        StopSpeech();
        return _navigationService.GoBackAsync();
    }

    public void StopSpeech() => _audioService.StopSpeech();
}
